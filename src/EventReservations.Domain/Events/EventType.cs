namespace EventReservations.Domain.Events;

/// <summary>
/// Tipo de evento (RF-01). Los valores corresponden a los tipos válidos del
/// enunciado: conferencia, taller, concierto.
/// </summary>
public enum EventType
{
    Conferencia = 0,
    Taller = 1,
    Concierto = 2
}
