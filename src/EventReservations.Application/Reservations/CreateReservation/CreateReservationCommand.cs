namespace EventReservations.Application.Reservations.CreateReservation;

/// <summary>
/// Comando para reservar entradas (RF-03). El comprador se identifica por nombre
/// y email (no hay autenticación, ver ADR-003).
/// </summary>
public sealed record CreateReservationCommand(
    Guid EventId,
    int Quantity,
    string BuyerName,
    string BuyerEmail);
