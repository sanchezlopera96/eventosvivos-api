using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EventReservations.Infrastructure.Persistence;

/// <summary>
/// Permite a las herramientas de EF (dotnet ef) crear el contexto en tiempo de
/// diseño para generar migraciones, sin necesidad de arrancar la API ni de
/// conectarse a una base de datos real. La cadena de conexión aquí es solo un
/// marcador: generar migraciones no abre conexión.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=eventosvivos;Username=postgres;Password=postgres")
            .Options;

        return new AppDbContext(options);
    }
}
