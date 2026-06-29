namespace EventReservations.Application.Common;

/// <summary>
/// Se lanza cuando el código de reserva generado colisiona con uno existente
/// (violación del índice único). La capa de aplicación la captura para
/// reintentar con un código nuevo (RF-04).
/// </summary>
public sealed class DuplicateReservationCodeException : Exception
{
    public DuplicateReservationCodeException(Exception inner)
        : base("El código de reserva generado ya existe.", inner) { }
}
