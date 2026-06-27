namespace EventReservations.Integration.Tests;

public sealed class TestClock : TimeProvider
{
    private readonly DateTimeOffset _now;

    public TestClock(DateTime utcNow)
        => _now = new DateTimeOffset(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc));

    public override DateTimeOffset GetUtcNow() => _now;
}
