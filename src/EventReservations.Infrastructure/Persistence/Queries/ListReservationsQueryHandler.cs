using EventReservations.Application.Abstractions;
using EventReservations.Application.Reservations.ListReservations;
using Microsoft.EntityFrameworkCore;

namespace EventReservations.Infrastructure.Persistence.Queries;

public sealed class ListReservationsQueryHandler
    : IQueryHandler<ListReservationsQuery, IReadOnlyList<ReservationListItemDto>>
{
    private readonly AppDbContext _db;

    public ListReservationsQueryHandler(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<ReservationListItemDto>> HandleAsync(
        ListReservationsQuery query, CancellationToken cancellationToken = default)
    {
        // 1) Materializa las reservas (filtradas por estado) en memoria. Al traer
        //    la entidad completa, el Value Object Code se hidrata sin problemas de
        //    traduccion SQL.
        var reservationsQuery = _db.Reservations.AsNoTracking().AsQueryable();
        if (query.Status is not null)
            reservationsQuery = reservationsQuery.Where(r => r.Status == query.Status);

        var reservations = await reservationsQuery
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        if (reservations.Count == 0)
            return [];

        // 2) Trae los titulos de los eventos involucrados en un solo query.
        var eventIds = reservations.Select(r => r.EventId).Distinct().ToList();
        var titles = await _db.Events.AsNoTracking()
            .Where(e => eventIds.Contains(e.Id))
            .Select(e => new { e.Id, e.Title })
            .ToDictionaryAsync(e => e.Id, e => e.Title, cancellationToken);

        // 3) Construye el DTO en memoria.
        return reservations
            .Select(r => new ReservationListItemDto(
                r.Id,
                r.EventId,
                titles.TryGetValue(r.EventId, out var title) ? title : string.Empty,
                r.Buyer.Name,
                r.Buyer.Email,
                r.Quantity,
                r.Status,
                r.Code?.Value,
                r.CreatedAt))
            .ToList();
    }
}
