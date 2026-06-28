using System.Net;
using System.Net.Http.Headers;
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

    // Fecha futura a las 18:00 (pasa RN03 cualquier dia) y en UTC.
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

    // Hace login con las credenciales de prueba y devuelve el token JWT.
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

    private async Task<HttpRequestMessage> PostAsync(string url, object body, bool withAuth)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body)
        };
        if (withAuth)
        {
            var token = await GetAdminTokenAsync();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        return request;
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        var token = await GetAdminTokenAsync();
        token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task CreateEvent_WithToken_Returns201()
    {
        var response = await _client.SendAsync(await PostAsync("/api/events", ValidEventBody(), withAuth: true));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateEvent_WithoutToken_Returns401()
    {
        var response = await _client.SendAsync(await PostAsync("/api/events", ValidEventBody(), withAuth: false));

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

        // Con token valido: la validacion (400) debe evaluarse igualmente.
        var response = await _client.SendAsync(await PostAsync("/api/events", body, withAuth: true));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ListEvents_IsPublic_Returns200()
    {
        var response = await _client.GetAsync("/api/events");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
