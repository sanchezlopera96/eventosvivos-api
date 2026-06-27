using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace EventReservations.Integration.Tests;

[Collection("api")]
public class EventsApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public EventsApiTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // Fecha futura a las 18:00 (pasa RN03 cualquier día) y en UTC.
    private static readonly DateTime Start =
        DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(90).AddHours(18), DateTimeKind.Utc);

    private static object ValidEventBody() => new
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

    private HttpRequestMessage Post(string url, object body, bool withApiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body)
        };
        if (withApiKey)
            request.Headers.Add("X-Api-Key", ApiFactory.AdminApiKey);
        return request;
    }

    [Fact]
    public async Task CreateEvent_WithApiKey_Returns201()
    {
        var response = await _client.SendAsync(Post("/api/events", ValidEventBody(), withApiKey: true));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateEvent_WithoutApiKey_Returns401()
    {
        var response = await _client.SendAsync(Post("/api/events", ValidEventBody(), withApiKey: false));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
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

        // Con API key válida: la validación (400) debe evaluarse igualmente.
        var response = await _client.SendAsync(Post("/api/events", body, withApiKey: true));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ListEvents_IsPublic_Returns200()
    {
        var response = await _client.GetAsync("/api/events");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
