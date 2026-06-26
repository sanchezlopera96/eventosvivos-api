using EventReservations.Domain.Common;
using EventReservations.Domain.Events;
using FluentAssertions;
using Xunit;

namespace EventReservations.Domain.Tests.Events;

public class ScheduleTests
{
    private static readonly DateTime Start = new(2026, 3, 10, 18, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_WhenEndBeforeStart_Throws()
    {
        var act = () => Schedule.Create(Start, Start.AddHours(-1));

        act.Should().Throw<DomainException>().WithMessage("*posterior*");
    }

    [Fact]
    public void Create_WhenEndEqualsStart_Throws()
    {
        var act = () => Schedule.Create(Start, Start);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_WhenValid_HoldsValues()
    {
        var end = Start.AddHours(2);

        var schedule = Schedule.Create(Start, end);

        schedule.StartsAt.Should().Be(Start);
        schedule.EndsAt.Should().Be(end);
    }
}
