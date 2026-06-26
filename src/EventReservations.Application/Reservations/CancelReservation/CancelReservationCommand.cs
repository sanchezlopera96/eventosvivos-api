namespace EventReservations.Application.Reservations.CancelReservation;

/// <summary>Comando para cancelar una reserva (RF-05).</summary>
public sealed record CancelReservationCommand(Guid ReservationId);
