using EventReservations.Application.Abstractions;
using EventReservations.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace EventReservations.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;

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
    }
}
