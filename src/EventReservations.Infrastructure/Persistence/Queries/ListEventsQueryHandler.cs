using EventReservations.Application.Abstractions;
using EventReservations.Application.Events.ListEvents;
using Microsoft.EntityFrameworkCore;

namespace EventReservations.Infrastructure.Persistence.Queries;

public sealed class ListEventsQueryHandler
    : IQueryHandler<ListEventsQuery, IReadOnlyList<EventListItemDto>>
{
    private readonly AppDbContext _db;
    private readonly TimeProvider _timeProvider;

    public ListEventsQueryHandler(AppDbContext db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<EventListItemDto>> HandleAsync(
        ListEventsQuery query, CancellationToken cancellationToken = default)
    {
        var q = _db.Events.AsNoTracking();

        if (query.Type is not null)
            q = q.Where(e => e.Type == query.Type.Value);

        if (query.VenueId is not null)
            q = q.Where(e => e.VenueId == query.VenueId.Value);

        // NOTA: el filtro por estado NO se aplica aquí. El estado efectivo (RN06)
        // se calcula en memoria tras materializar, así que filtrar por "completado"
        // en SQL omitiría eventos que en BD siguen "activo" pero cuyo fin ya pasó.

        if (query.StartsFrom is not null)
            q = q.Where(e => e.Schedule.StartsAt >= query.StartsFrom.Value);

        if (query.StartsTo is not null)
            q = q.Where(e => e.Schedule.StartsAt <= query.StartsTo.Value);

        if (!string.IsNullOrWhiteSpace(query.TitleContains))
        {
            // Escapamos los comodines de LIKE (%, _, \) para que se traten como
            // texto literal y no como patrones (RF-02: búsqueda parcial).
            var term = query.TitleContains
                .Replace("\\", "\\\\")
                .Replace("%", "\\%")
                .Replace("_", "\\_");
            q = q.Where(e => EF.Functions.ILike(e.Title, $"%{term}%", "\\")); // case-insensitive
        }

        // Materializamos (EF aplica los convertidores de los VOs) y mapeamos en memoria.
        var events = await q.OrderBy(e => e.Schedule.StartsAt).ToListAsync(cancellationToken);

        // RN06: estado efectivo en lectura.
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var items = events
            .Select(e => new EventListItemDto(
                e.Id, e.Title, e.VenueId, e.Type, e.EffectiveStatus(now),
                e.Schedule.StartsAt, e.Schedule.EndsAt,
                e.Price.Amount, e.Capacity.Value, e.AvailableSeats));

        // Filtro por estado aplicado sobre el estado EFECTIVO (en memoria).
        if (query.Status is not null)
            items = items.Where(i => i.Status == query.Status.Value);

        return items.ToList();
    }
}
