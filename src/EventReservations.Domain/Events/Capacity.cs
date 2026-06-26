using EventReservations.Domain.Common;

namespace EventReservations.Domain.Events;

/// <summary>
/// Value Object de capacidad. Invariante: entero positivo (RF-01, RN01).
/// </summary>
public sealed record Capacity
{
    public int Value { get; }

    private Capacity(int value) => Value = value;

    public static Capacity Of(int value)
        => value <= 0
            ? throw new DomainException("La capacidad debe ser un entero mayor que cero.")
            : new Capacity(value);
}
