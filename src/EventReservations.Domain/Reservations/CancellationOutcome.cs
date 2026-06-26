namespace EventReservations.Domain.Reservations;

/// <summary>
/// Resultado de cancelar una reserva, respecto al aforo del evento:
///   SeatsReleased  -> las plazas vuelven a estar disponibles (cancelación >= 48h)
///   SeatsForfeited -> las plazas se pierden, no se liberan para venta (RN07, < 48h)
///
/// El agregado Reservation decide el resultado según el tiempo restante al evento;
/// el caso de uso aplica el efecto sobre los contadores de Event.
/// </summary>
public enum CancellationOutcome
{
    SeatsReleased = 0,
    SeatsForfeited = 1
}
