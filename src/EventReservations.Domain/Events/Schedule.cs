using EventReservations.Domain.Common;

namespace EventReservations.Domain.Events;

/// <summary>
/// Value Object con la ventana temporal del evento.
/// Invariante estructural: el fin debe ser posterior al inicio (RF-01).
///
/// Las reglas que dependen del momento actual (fecha futura) o de políticas de
/// negocio (RN03: horario nocturno en fin de semana) se validan en el agregado
/// Event al crearlo, no aquí, para mantener este VO puramente estructural.
///
/// Nota de alcance: el sistema opera en una única zona horaria (la de los venues,
/// Colombia). No se modela conversión multi-zona; las fechas se interpretan en la
/// hora local del venue. El soporte multi-zona queda fuera de alcance.
/// </summary>
public sealed record Schedule
{
    public DateTime StartsAt { get; }
    public DateTime EndsAt { get; }

    private Schedule(DateTime startsAt, DateTime endsAt)
    {
        StartsAt = startsAt;
        EndsAt = endsAt;
    }

    public static Schedule Create(DateTime startsAt, DateTime endsAt)
        => endsAt <= startsAt
            ? throw new DomainException("La fecha de fin debe ser posterior a la de inicio.")
            : new Schedule(startsAt, endsAt);
}
