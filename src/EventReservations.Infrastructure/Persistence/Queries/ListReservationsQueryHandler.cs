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
        var reservations = _db.Reservations.AsNoTracking().AsQueryable();

        if (query.Status is not null)
            reservations = reservations.Where(r => r.Status == query.Status);

        // Join con eventos para incluir el titulo del evento.
        var result = await reservations
            .Join(_db.Events.AsNoTracking(),
                r => r.EventId,
                e => e.Id,
                (r, e) => new ReservationListItemDto(
                    r.Id,
                    r.EventId,
                    e.Title,
                    r.Buyer.Name,
                    r.Buyer.Email,
                    r.Quantity,
                    r.Status,
                    r.Code != null ? r.Code.Value : null,
                    r.CreatedAt))
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return result;
    }
}
