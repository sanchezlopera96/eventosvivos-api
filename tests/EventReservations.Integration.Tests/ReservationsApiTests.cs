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

    private static readonly DateTime Start =
        DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(120).AddHours(18), DateTimeKind.Utc);

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

    // Crea un evento (admin) y devuelve su id.
    private async Task<Guid> CreateEventAsync(string token)
    {
        Authorize(token);
        var response = await _client.PostAsJsonAsync("/api/events", new
        {
            title = "Evento para reservas",
            description = "Evento usado por los tests de listado de reservas.",
            venueId = 1,
            capacity = 100,
            startsAt = Start,
            endsAt = Start.AddHours(2),
            price = 50m,
            type = 2
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<CreatedPayload>();
        return created!.Id;
    }

    // Crea una reserva (publico) y devuelve su id.
    private async Task<Guid> CreateReservationAsync(Guid eventId)
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.PostAsJsonAsync("/api/reservations", new
        {
            eventId,
            quantity = 2,
            buyerName = "Comprador de Prueba",
            buyerEmail = "comprador@test.com"
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

    private sealed record ReservationListItem(
        Guid Id, Guid EventId, string EventTitle, string BuyerName,
        string BuyerEmail, int Quantity, int Status, string? Code, DateTime CreatedAt);
}
