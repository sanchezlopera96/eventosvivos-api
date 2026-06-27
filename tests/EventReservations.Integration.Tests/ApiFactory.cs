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
/// Levanta la API real (Program) en memoria contra un PostgreSQL efímero de
/// Testcontainers. Inyecta la API key de admin por configuración y reemplaza el
/// registro del DbContext para apuntarlo al contenedor (esto se hace en
/// ConfigureTestServices, que se ejecuta después del registro de la app, por lo
/// que siempre gana, sin depender del orden de carga de configuración).
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string AdminApiKey = "test-admin-key";

    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder("postgres:16").Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:AdminApiKey"] = AdminApiKey,
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
