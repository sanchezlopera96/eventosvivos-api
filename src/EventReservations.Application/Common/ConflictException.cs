namespace EventReservations.Application.Common;

/// <summary>
/// Se lanza cuando una operación no puede completarse por un conflicto de
/// concurrencia (p. ej. dos reservas compitiendo por las últimas plazas, detectado
/// por el token xmin). La API la traduce a HTTP 409 Conflict.
/// </summary>
public sealed class ConflictException : Exception
{
    public ConflictException(string message, Exception? innerException = null)
        : base(message, innerException) { }
}
