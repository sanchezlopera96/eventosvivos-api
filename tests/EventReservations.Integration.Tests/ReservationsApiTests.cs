using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace EventReservations.Integration.Tests;

[Collection("api")]
public class ReservationsApiTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public ReservationsApiTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
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

    // Genera una fecha de inicio futura unica por evento para evitar el
    // solapamiento de eventos en la misma sede (RN02) entre tests que comparten
    // la misma base de datos.
    private static DateTime UniqueStart()
    {
        var daysAhead = Random.Shared.Next(200, 5000);
        return DateTime.SpecifyKind(
            DateTime.UtcNow.Date.AddDays(daysAhead).AddHours(18), DateTimeKind.Utc);
    }

    // Crea un evento (admin) y devuelve su id.
    private async Task<Guid> CreateEventAsync(string token)
    {
        Authorize(token);
        var start = UniqueStart();
        var response = await _client.PostAsJsonAsync("/api/events", new
        {
            title = "Evento para reservas",
            description = "Evento usado por los tests de listado de reservas.",
            venueId = 1,
            capacity = 100,
            startsAt = start,
            endsAt = start.AddHours(2),
            price = 50m,
            type = 2
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<CreatedPayload>();
        return created!.Id;
    }

    // Crea una reserva (publico) y devuelve su id. Permite especificar el correo.
    private async Task<Guid> CreateReservationAsync(Guid eventId, string email = "comprador@test.com")
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.PostAsJsonAsync("/api/reservations", new
        {
            eventId,
            quantity = 2,
            buyerName = "Comprador de Prueba",
            buyerEmail = email
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<CreatedPayload>();
        return created!.Id;
    }

    [Fact]
    public async Task ListReservations_WithoutToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.GetAsync("/api/reservations?status=0");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListPendingReservations_WithToken_ReturnsCreatedReservation()
    {
        var token = await GetAdminTokenAsync();
        var eventId = await CreateEventAsync(token);
        var reservationId = await CreateReservationAsync(eventId);

        // Lista reservas pendientes (status 0). La reserva recien creada esta
        // pendiente y su codigo (Code) es null: este es justo el caso que antes
        // provocaba un 500 al proyectar el Value Object.
        Authorize(token);
        var response = await _client.GetAsync("/api/reservations?status=0");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await response.Content.ReadFromJsonAsync<List<ReservationListItem>>();
        items.Should().NotBeNull();
        items!.Should().Contain(r => r.Id == reservationId);
        items.Should().OnlyContain(r => r.Status == 0);
    }

    [Fact]
    public async Task SearchByEmail_IsPublic_ReturnsBuyerReservations()
    {
        var token = await GetAdminTokenAsync();
        var eventId = await CreateEventAsync(token);

        // Correo unico para aislar este test de otras reservas en la misma BD.
        var email = $"buscar-{Guid.NewGuid():N}@test.com";
        var reservationId = await CreateReservationAsync(eventId, email);

        // Endpoint publico: sin token.
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.GetAsync($"/api/reservations/by-email?email={Uri.EscapeDataString(email)}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await response.Content.ReadFromJsonAsync<List<ReservationListItem>>();
        items.Should().NotBeNull();
        items!.Should().ContainSingle(r => r.Id == reservationId);
        items.Should().OnlyContain(r => r.BuyerEmail == email);
    }

    [Fact]
    public async Task SearchByEmail_WhenNoReservations_ReturnsEmptyList()
    {
        var email = $"sin-reservas-{Guid.NewGuid():N}@test.com";

        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.GetAsync($"/api/reservations/by-email?email={Uri.EscapeDataString(email)}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await response.Content.ReadFromJsonAsync<List<ReservationListItem>>();
        items.Should().NotBeNull();
        items!.Should().BeEmpty();
    }

    private sealed record ReservationListItem(
        Guid Id, Guid EventId, string EventTitle, string BuyerName,
        string BuyerEmail, int Quantity, int Status, string? Code, DateTime CreatedAt);
}
