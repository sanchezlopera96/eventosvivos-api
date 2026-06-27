using EventReservations.Domain.Events;

namespace EventReservations.Application.Events.GetEventById;

public sealed record EventDetailDto(
    Guid Id,
    string Title,
    string Description,
    int VenueId,
    EventType Type,
    EventStatus Status,
    DateTime StartsAt,
    DateTime EndsAt,
    decimal Price,
    int Capacity,
    int AvailableSeats);
