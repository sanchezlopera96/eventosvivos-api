using EventReservations.Domain.Events;

namespace EventReservations.Application.Abstractions;

public interface IEventRepository
{
    Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(Event @event, CancellationToken cancellationToken = default);

    /// <summary>
    /// RN02: indica si el venue ya tiene un evento activo cuyo horario se solapa
    /// con la ventana indicada. Se usa al crear un evento para impedir choques.
    /// </summary>
    Task<bool> ExistsActiveOverlapAsync(
        int venueId, DateTime startsAt, DateTime endsAt,
        CancellationToken cancellationToken = default);
}
