namespace EventReservations.Application.Tests.Common;

/// <summary>
/// TimeProvider con un instante fijo, para tests deterministas del reloj.
/// </summary>
public sealed class FixedTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _now;

    public FixedTimeProvider(DateTime utcNow)
        => _now = new DateTimeOffset(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc));

    public override DateTimeOffset GetUtcNow() => _now;
}
