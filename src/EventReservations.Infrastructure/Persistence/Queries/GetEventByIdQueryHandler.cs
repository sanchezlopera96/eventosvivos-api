using EventReservations.Application.Abstractions;
using EventReservations.Application.Common;
using EventReservations.Application.Events.GetEventById;
using Microsoft.EntityFrameworkCore;

namespace EventReservations.Infrastructure.Persistence.Queries;

public sealed class GetEventByIdQueryHandler
    : IQueryHandler<GetEventByIdQuery, EventDetailDto>
{
    private readonly AppDbContext _db;

    public GetEventByIdQueryHandler(AppDbContext db) => _db = db;

    public async Task<EventDetailDto> HandleAsync(
        GetEventByIdQuery query, CancellationToken cancellationToken = default)
    {
        var e = await _db.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken)
            ?? throw NotFoundException.For("evento", query.Id);

        return new EventDetailDto(
            e.Id, e.Title, e.Description, e.VenueId, e.Type, e.Status,
            e.Schedule.StartsAt, e.Schedule.EndsAt,
            e.Price.Amount, e.Capacity.Value, e.AvailableSeats);
    }
}
