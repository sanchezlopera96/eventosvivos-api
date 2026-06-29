using EventReservations.Application.Abstractions;
using EventReservations.Application.Common;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EventReservations.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;

    // Índice único del código de reserva (convención EF: IX_<tabla>_<propiedad>).
    private const string ReservationCodeIndex = "IX_reservations_Code";
    // SqlState de PostgreSQL para violación de restricción de unicidad.
    private const string UniqueViolation = "23505";

    public UnitOfWork(AppDbContext db) => _db = db;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // ADR-006: el token xmin detectó que otra transacción modificó el
            // evento (p. ej. otra reserva tomó las últimas plazas). Se traduce a
            // un conflicto de aplicación -> HTTP 409.
            throw new ConflictException(
                "La operación no pudo completarse por un conflicto de concurrencia. Vuelve a intentarlo.",
                ex);
        }
        catch (DbUpdateException ex) when (IsReservationCodeCollision(ex))
        {
            // RF-04: el código EV-###### generado colisionó con uno existente.
            // La capa de aplicación la captura y reintenta con un código nuevo.
            throw new DuplicateReservationCodeException(ex);
        }
    }

    private static bool IsReservationCodeCollision(DbUpdateException ex)
        => ex.InnerException is PostgresException pg
           && pg.SqlState == UniqueViolation
           && pg.ConstraintName == ReservationCodeIndex;
}
