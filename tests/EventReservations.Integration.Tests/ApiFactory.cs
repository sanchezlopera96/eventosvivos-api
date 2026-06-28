using EventReservations.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace EventReservations.Integration.Tests;

/// <summary>
/// Levanta la API real (Program) en memoria contra un PostgreSQL efimero de
/// Testcontainers. Inyecta la configuracion JWT y las credenciales de admin de
/// prueba por configuracion, y reemplaza el registro del DbContext para
/// apuntarlo al contenedor (en ConfigureTestServices, que se ejecuta despues
/// del registro de la app, por lo que siempre gana).
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Credenciales de administracion de prueba.
    public const string AdminUsername = "admin";
    public const string AdminPassword = "test-password";
    // Hash BCrypt de "test-password".
    private const string AdminPasswordHash = "$2b$11$vsMppNRCleO0FBWsT9b4CuEX90yzOriqAYMFh75zIto1VGygBjzcq";

    // Clave de firma JWT de prueba (>= 32 chars).
    public const string JwtSigningKey = "test-jwt-signing-key-con-longitud-suficiente-2026";

    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder("postgres:16").Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "eventosvivos",
                ["Jwt:Audience"] = "eventosvivos-admin",
                ["Jwt:SigningKey"] = JwtSigningKey,
                ["Jwt:ExpirationMinutes"] = "120",
                ["AdminCredentials:Username"] = AdminUsername,
                ["AdminCredentials:PasswordHash"] = AdminPasswordHash,
                ["Swagger:Enabled"] = "false"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Quita el DbContext registrado por la app y lo reapunta al contenedor.
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(_db.GetConnectionString()));
        });
    }

    async Task IAsyncLifetime.InitializeAsync() => await _db.StartAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _db.DisposeAsync();
        await base.DisposeAsync();
    }
}
