using EventReservations.Application.Abstractions;
using EventReservations.Application.Reservations.ListReservations;
using EventReservations.Application.Reservations.ListReservationsByEmail;
using Microsoft.EntityFrameworkCore;

namespace EventReservations.Infrastructure.Persistence.Queries;

public sealed class ListReservationsByEmailQueryHandler
    : IQueryHandler<ListReservationsByEmailQuery, IReadOnlyList<ReservationListItemDto>>
{
    private readonly AppDbContext _db;

    public ListReservationsByEmailQueryHandler(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<ReservationListItemDto>> HandleAsync(
        ListReservationsByEmailQuery query, CancellationToken cancellationToken = default)
    {
        var email = query.Email.Trim().ToLowerInvariant();

        // Materializa las reservas del correo (comparacion case-insensitive),
        // luego resuelve titulos y construye el DTO en memoria (acceso seguro al
        // Value Object Code, igual que en el listado de admin).
        var reservations = await _db.Reservations.AsNoTracking()
            .Where(r => r.Buyer.Email.ToLower() == email)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        if (reservations.Count == 0)
            return [];

        var eventIds = reservations.Select(r => r.EventId).Distinct().ToList();
        var titles = await _db.Events.AsNoTracking()
            .Where(e => eventIds.Contains(e.Id))
            .Select(e => new { e.Id, e.Title })
            .ToDictionaryAsync(e => e.Id, e => e.Title, cancellationToken);

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
