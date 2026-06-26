namespace EventReservations.Domain.Common;

/// <summary>
/// Value Object monetario. Invariante: importe positivo (RF-01, precio de entrada).
/// </summary>
public sealed record Money
{
    public decimal Amount { get; }

    private Money(decimal amount) => Amount = amount;

    public static Money Of(decimal amount)
        => amount <= 0
            ? throw new DomainException("El importe debe ser un valor positivo.")
            : new Money(amount);
}
