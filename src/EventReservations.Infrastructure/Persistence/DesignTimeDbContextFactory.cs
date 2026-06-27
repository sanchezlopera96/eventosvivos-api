using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EventReservations.Infrastructure.Persistence;

/// <summary>
/// Permite a las herramientas de EF (dotnet ef) crear el contexto en tiempo de
/// diseño para generar/aplicar migraciones, sin arrancar la API.
///
/// La cadena de conexión se toma de la variable de entorno
/// <c>ConnectionStrings__Default</c>; si no está definida, usa un valor por
/// defecto SOLO para desarrollo local (base efímera en localhost). No hay
/// secretos productivos en el repositorio: en cualquier entorno real la cadena
/// se inyecta por configuración/variable de entorno.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    private const string LocalDevelopmentConnectionString =
        "Host=localhost;Port=5432;Database=eventosvivos;Username=postgres;Password=postgres";

    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? LocalDevelopmentConnectionString;

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new AppDbContext(options);
    }
}
