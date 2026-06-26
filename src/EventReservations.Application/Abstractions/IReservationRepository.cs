using EventReservations.Domain.Reservations;

namespace EventReservations.Application.Abstractions;

public interface IReservationRepository
{
    Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(Reservation reservation, CancellationToken cancellationToken = default);

    /// <summary>Comprueba si ya existe una reserva con ese código (unicidad RF-04).</summary>
    Task<bool> ExistsCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Devuelve las reservas no canceladas de un evento (para la cascada al
    /// cancelar el evento).
    /// </summary>
    Task<IReadOnlyList<Reservation>> ListActiveByEventAsync(
        Guid eventId, CancellationToken cancellationToken = default);
}
