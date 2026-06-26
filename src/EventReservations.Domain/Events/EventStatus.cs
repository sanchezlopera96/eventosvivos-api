namespace EventReservations.Domain.Events;

/// <summary>
/// Estado del evento (RF-02 filtro, RN06).
///   Activo      -> Cancelado | Completado
///   Cancelado   -> (terminal)
///   Completado  -> (terminal)
/// </summary>
public enum EventStatus
{
    Activo = 0,
    Cancelado = 1,
    Completado = 2
}
