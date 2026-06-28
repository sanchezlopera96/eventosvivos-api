using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace EventReservations.Integration.Tests;

[Collection("api")]
public class UpdateEventApiTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public UpdateEventApiTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    // Fecha futura unica por evento (evita solapamientos RN02 en la BD compartida).
    private static DateTime UniqueStart()
    {
        var daysAhead = Random.Shared.Next(200, 5000);
        return DateTime.SpecifyKind(
            DateTime.UtcNow.Date.AddDays(daysAhead).AddHours(18), DateTimeKind.Utc);
    }

    private async Task<string> GetAdminTokenAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            username = ApiFactory.AdminUsername,
            password = ApiFactory.AdminPassword
        });
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<LoginPayload>();
        return payload!.Token;
    }

    private sealed record LoginPayload(string Token, DateTime ExpiresAt);
    private sealed record CreatedPayload(Guid Id);

    private void Authorize(string token) =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    // Crea un evento y devuelve (id, start). venueId 1 = Auditorio (aforo 200).
    private async Task<(Guid Id, DateTime Start)> CreateEventAsync(
        string token, int venueId = 1, int capacity = 100)
    {
        Authorize(token);
        var start = UniqueStart();
        var response = await _client.PostAsJsonAsync("/api/events", new
        {
            title = "Evento original",
            description = "Evento creado para los tests de edicion.",
            venueId,
            capacity,
            startsAt = start,
            endsAt = start.AddHours(2),
            price = 50m,
            type = 2
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<CreatedPayload>();
        return (created!.Id, start);
    }

    private static object UpdateBody(
        DateTime start, int venueId = 1, int capacity = 120,
        string title = "Evento editado correctamente",
        decimal price = 75m) => new
    {
        title,
        description = "Descripcion editada por el test de integracion.",
        venueId,
        capacity,
        startsAt = start,
        endsAt = start.AddHours(3),
        price,
        type = 1
    };

    [Fact]
    public async Task UpdateEvent_WithoutToken_Returns401()
    {
        var token = await GetAdminTokenAsync();
        var (id, start) = await CreateEventAsync(token);

        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.PutAsJsonAsync($"/api/events/{id}", UpdateBody(start));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateEvent_WithValidData_UpdatesEvent()
    {
        var token = await GetAdminTokenAsync();
        var (id, start) = await CreateEventAsync(token);

        Authorize(token);
        var newStart = UniqueStart();
        var response = await _client.PutAsJsonAsync($"/api/events/{id}",
            UpdateBody(newStart, capacity: 150, title: "Titulo nuevo del evento"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verifica que los cambios se reflejan al consultar el evento.
        var detail = await _client.GetFromJsonAsync<EventDetail>($"/api/events/{id}");
        detail!.Title.Should().Be("Titulo nuevo del evento");
        detail.Capacity.Should().Be(150);
        detail.Type.Should().Be(1);
    }

    [Fact]
    public async Task UpdateEvent_WhenCapacityBelowSold_Returns422()
    {
        var token = await GetAdminTokenAsync();
        var (id, start) = await CreateEventAsync(token, capacity: 100);

        // Crea una reserva de 10 entradas (publico): ocupa 10 plazas.
        _client.DefaultRequestHeaders.Authorization = null;
        var reserve = await _client.PostAsJsonAsync("/api/reservations", new
        {
            eventId = id,
            quantity = 10,
            buyerName = "Comprador Prueba",
            buyerEmail = "comprador-edit@test.com"
        });
        reserve.StatusCode.Should().Be(HttpStatusCode.Created);

        // Intenta reducir la capacidad a 5 (< 10 ocupadas) -> debe fallar.
        Authorize(token);
        var response = await _client.PutAsJsonAsync($"/api/events/{id}",
            UpdateBody(start, capacity: 5));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task UpdateEvent_WhenOverlapsAnotherEvent_Returns422()
    {
        var token = await GetAdminTokenAsync();

        // Dos eventos en el mismo venue, en horarios distintos.
        var (_, startA) = await CreateEventAsync(token, venueId: 1);
        var (idB, _) = await CreateEventAsync(token, venueId: 1);

        // Intenta mover el evento B al horario del evento A -> solapamiento.
        Authorize(token);
        var response = await _client.PutAsJsonAsync($"/api/events/{idB}",
            UpdateBody(startA, venueId: 1));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task UpdateEvent_WhenNotFound_Returns404()
    {
        var token = await GetAdminTokenAsync();
        Authorize(token);

        var response = await _client.PutAsJsonAsync($"/api/events/{Guid.NewGuid()}",
            UpdateBody(UniqueStart()));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed record EventDetail(
        Guid Id, string Title, string Description, int VenueId,
        int Capacity, int Type, int Status);
}
