using System.Text.RegularExpressions;
using EventReservations.Domain.Common;

namespace EventReservations.Domain.Reservations;

/// <summary>
/// Código único de reserva con formato EV-{6 dígitos} (RF-04).
/// El VO garantiza el formato; la unicidad es responsabilidad de la capa de
/// persistencia (índice único) y de aplicación (reintento ante colisión).
/// </summary>
public sealed partial record ReservationCode
{
    public string Value { get; }

    private ReservationCode(string value) => Value = value;

    /// <summary>Genera un código nuevo con 6 dígitos aleatorios.</summary>
    public static ReservationCode Generate()
    {
        var number = Random.Shared.Next(0, 1_000_000);
        return new ReservationCode($"EV-{number:D6}");
    }

    /// <summary>Reconstruye un código existente validando su formato.</summary>
    public static ReservationCode From(string value)
    {
        if (value is null || !CodeRegex().IsMatch(value))
            throw new DomainException("El código de reserva no tiene un formato válido (EV-######).");

        return new ReservationCode(value);
    }

    [GeneratedRegex(@"^EV-\d{6}$")]
    private static partial Regex CodeRegex();
}
