namespace EventReservations.Domain.Reservations;

/// <summary>
/// Estados de una reserva (enunciado):
///   PendientePago -> Confirmada -> Cancelada
/// `Confirmada` equivale a "pagada".
/// </summary>
public enum ReservationStatus
{
    PendientePago = 0,
    Confirmada = 1,
    Cancelada = 2
}
