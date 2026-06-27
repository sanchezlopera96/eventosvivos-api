using EventReservations.Application.Abstractions;
using EventReservations.Application.Common;
using EventReservations.Application.Reservations.GetReservationById;
using Microsoft.EntityFrameworkCore;

namespace EventReservations.Infrastructure.Persistence.Queries;

public sealed class GetReservationByIdQueryHandler
    : IQueryHandler<GetReservationByIdQuery, ReservationDetailDto>
{
    private readonly AppDbContext _db;

    public GetReservationByIdQueryHandler(AppDbContext db) => _db = db;

    public async Task<ReservationDetailDto> HandleAsync(
        GetReservationByIdQuery query, CancellationToken cancellationToken = default)
    {
        var r = await _db.Reservations
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken)
            ?? throw NotFoundException.For("reserva", query.Id);

        return new ReservationDetailDto(
            r.Id,
            r.EventId,
            r.Buyer.Name,
            r.Buyer.Email,
            r.Quantity,
            r.Status,
            r.Code?.Value,
            r.CreatedAt,
            r.ConfirmedAt);
    }
}
