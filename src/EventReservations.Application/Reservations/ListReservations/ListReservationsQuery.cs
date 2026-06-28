using EventReservations.Domain.Reservations;

namespace EventReservations.Application.Reservations.ListReservations;

public sealed record ListReservationsQuery(ReservationStatus? Status);

public sealed record ReservationListItemDto(
    Guid Id,
    Guid EventId,
    string EventTitle,
    string BuyerName,
    string BuyerEmail,
    int Quantity,
    ReservationStatus Status,
    string? Code,
    DateTime CreatedAt);
