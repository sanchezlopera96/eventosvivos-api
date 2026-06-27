using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace EventReservations.Integration.Tests;

[Collection("api")]
public class EventsApiTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public EventsApiTests(ApiFactory factory) => _client = factory.CreateClient();

    // Fecha futura a las 18:00 (pasa RN03 cualquier día) y en UTC.
    private static readonly DateTime Start =
        DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(90).AddHours(18), DateTimeKind.Utc);

    [Fact]
    public async Task CreateEvent_WhenValid_Returns201()
    {
        var body = new
        {
            title = "Concierto de Integracion",
            description = "Evento de prueba creado por el test de integracion de la API.",
            venueId = 1,
            capacity = 100,
            startsAt = Start,
            endsAt = Start.AddHours(2),
            price = 50m,
            type = 2 // Concierto
        };

        var response = await _client.PostAsJsonAsync("/api/events", body);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateEvent_WhenTitleTooShort_Returns400()
    {
        var body = new
        {
            title = "Jaz", // < 5 caracteres
            description = "Descripcion suficientemente larga para el test.",
            venueId = 1,
            capacity = 100,
            startsAt = Start,
            endsAt = Start.AddHours(2),
            price = 50m,
            type = 2
        };

        var response = await _client.PostAsJsonAsync("/api/events", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ListEvents_Returns200()
    {
        var response = await _client.GetAsync("/api/events");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
