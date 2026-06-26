namespace EventReservations.Application.Reservations.ConfirmPayment;

/// <summary>Comando para confirmar el pago de una reserva (RF-04).</summary>
public sealed record ConfirmPaymentCommand(Guid ReservationId);
