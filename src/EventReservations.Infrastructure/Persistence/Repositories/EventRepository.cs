using EventReservations.Application.Abstractions;
using EventReservations.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace EventReservations.Infrastructure.Persistence.Repositories;

public sealed class EventRepository : IEventRepository
{
    private readonly AppDbContext _db;

    public EventRepository(AppDbContext db) => _db = db;

    public Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.Events.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task AddAsync(Event @event, CancellationToken cancellationToken = default)
        => await _db.Events.AddAsync(@event, cancellationToken);

    /// <summary>
    /// RN02: ¿hay algún evento activo en el mismo venue cuyo horario se solapa?
    /// Dos intervalos [a1,a2) y [b1,b2) se solapan si a1 &lt; b2 y b1 &lt; a2.
    /// Al editar se excluye el propio evento mediante excludeEventId.
    /// </summary>
    public Task<bool> ExistsActiveOverlapAsync(
        int venueId, DateTime startsAt, DateTime endsAt,
        Guid? excludeEventId = null, CancellationToken cancellationToken = default)
        => _db.Events.AnyAsync(
            e => e.VenueId == venueId
                 && e.Status == EventStatus.Activo
                 && (excludeEventId == null || e.Id != excludeEventId)
                 && e.Schedule.StartsAt < endsAt
                 && startsAt < e.Schedule.EndsAt,
            cancellationToken);
}
