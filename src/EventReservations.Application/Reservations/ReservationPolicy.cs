namespace EventReservations.Application.Reservations;

/// <summary>
/// Políticas de reserva que dependen del contexto (precio del evento y tiempo
/// restante para el inicio). Se aíslan aquí para ser testeables de forma directa
/// y para dejar explícita la PRIORIDAD entre reglas en conflicto.
///
///   RN04: no se reserva si faltan menos de 1 hora.
///   RF-03: si faltan menos de 24 horas, máximo 5 entradas por transacción.
///   RN05: si el precio &gt; $100, máximo 10 entradas por transacción.
///
/// RF-03 tiene prioridad sobre RN05 (enunciado): si ambas aplican, manda el
/// límite de 5.
/// </summary>
public static class ReservationPolicy
{
    public static readonly TimeSpan MinimumLeadTime = TimeSpan.FromHours(1);   // RN04
    public static readonly TimeSpan LateWindow = TimeSpan.FromHours(24);       // RF-03
    public const int LateWindowMaxTickets = 5;                                 // RF-03
    public const decimal HighPriceThreshold = 100m;                            // RN05
    public const int HighPriceMaxTickets = 10;                                 // RN05

    /// <summary>RN04: ¿se permite reservar según la antelación al evento?</summary>
    public static bool ReservationsAllowed(DateTime eventStartsAt, DateTime now)
        => eventStartsAt - now >= MinimumLeadTime;

    /// <summary>
    /// Máximo de entradas por transacción. Devuelve null si no hay límite
    /// (más allá de la disponibilidad del evento). RF-03 se evalúa primero,
    /// dándole prioridad sobre RN05.
    /// </summary>
    public static int? MaxTicketsPerTransaction(decimal price, DateTime eventStartsAt, DateTime now)
    {
        var timeUntilStart = eventStartsAt - now;

        if (timeUntilStart < LateWindow)       // RF-03 (prioridad sobre RN05)
            return LateWindowMaxTickets;

        if (price > HighPriceThreshold)         // RN05
            return HighPriceMaxTickets;

        return null;
    }
}
