using EventReservations.Domain.Events;

namespace EventReservations.Application.Events.ListEvents;

/// <summary>Elemento del listado de eventos (RF-02).</summary>
public sealed record EventListItemDto(
    Guid Id,
    string Title,
    int VenueId,
    EventType Type,
    EventStatus Status,
    DateTime StartsAt,
    DateTime EndsAt,
    decimal Price,
    int Capacity,
    int AvailableSeats);
