using EventReservations.Application.Abstractions;
using EventReservations.Application.Common;
using EventReservations.Application.Events.GetEventById;
using Microsoft.EntityFrameworkCore;

namespace EventReservations.Infrastructure.Persistence.Queries;

public sealed class GetEventByIdQueryHandler
    : IQueryHandler<GetEventByIdQuery, EventDetailDto>
{
    private readonly AppDbContext _db;
    private readonly TimeProvider _timeProvider;

    public GetEventByIdQueryHandler(AppDbContext db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    public async Task<EventDetailDto> HandleAsync(
        GetEventByIdQuery query, CancellationToken cancellationToken = default)
    {
        var e = await _db.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken)
            ?? throw NotFoundException.For("evento", query.Id);

        // RN06: estado efectivo en lectura (Completado si su fin ya pasó).
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var status = e.EffectiveStatus(now);

        return new EventDetailDto(
            e.Id, e.Title, e.Description, e.VenueId, e.Type, status,
            e.Schedule.StartsAt, e.Schedule.EndsAt,
            e.Price.Amount, e.Capacity.Value, e.AvailableSeats);
    }
}
