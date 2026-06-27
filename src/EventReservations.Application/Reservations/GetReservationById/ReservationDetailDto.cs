using EventReservations.Domain.Reservations;

namespace EventReservations.Application.Reservations.GetReservationById;

public sealed record ReservationDetailDto(
    Guid Id,
    Guid EventId,
    string BuyerName,
    string BuyerEmail,
    int Quantity,
    ReservationStatus Status,
    string? Code,
    DateTime CreatedAt,
    DateTime? ConfirmedAt);
