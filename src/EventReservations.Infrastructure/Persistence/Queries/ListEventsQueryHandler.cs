using EventReservations.Application.Abstractions;
using EventReservations.Application.Events.ListEvents;
using Microsoft.EntityFrameworkCore;

namespace EventReservations.Infrastructure.Persistence.Queries;

public sealed class ListEventsQueryHandler
    : IQueryHandler<ListEventsQuery, IReadOnlyList<EventListItemDto>>
{
    private readonly AppDbContext _db;

    public ListEventsQueryHandler(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<EventListItemDto>> HandleAsync(
        ListEventsQuery query, CancellationToken cancellationToken = default)
    {
        var q = _db.Events.AsNoTracking();

        if (query.Type is not null)
            q = q.Where(e => e.Type == query.Type.Value);

        if (query.VenueId is not null)
            q = q.Where(e => e.VenueId == query.VenueId.Value);

        if (query.Status is not null)
            q = q.Where(e => e.Status == query.Status.Value);

        if (query.StartsFrom is not null)
            q = q.Where(e => e.Schedule.StartsAt >= query.StartsFrom.Value);

        if (query.StartsTo is not null)
            q = q.Where(e => e.Schedule.StartsAt <= query.StartsTo.Value);

        if (!string.IsNullOrWhiteSpace(query.TitleContains))
            q = q.Where(e => EF.Functions.ILike(e.Title, $"%{query.TitleContains}%")); // case-insensitive

        // Materializamos (EF aplica los convertidores de los VOs) y mapeamos en memoria.
        var events = await q.OrderBy(e => e.Schedule.StartsAt).ToListAsync(cancellationToken);

        return events
            .Select(e => new EventListItemDto(
                e.Id, e.Title, e.VenueId, e.Type, e.Status,
                e.Schedule.StartsAt, e.Schedule.EndsAt,
                e.Price.Amount, e.Capacity.Value, e.AvailableSeats))
            .ToList();
    }
}
