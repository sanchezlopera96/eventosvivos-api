namespace EventReservations.Application.Abstractions;

/// <summary>
/// Confirma los cambios pendientes como una unidad atómica. Permite a los casos
/// de uso persistir varias modificaciones (p. ej. evento + reserva) en una sola
/// transacción, sin acoplarse a EF Core.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
