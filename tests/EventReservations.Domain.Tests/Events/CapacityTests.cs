using EventReservations.Domain.Common;
using EventReservations.Domain.Events;
using FluentAssertions;
using Xunit;

namespace EventReservations.Domain.Tests.Events;

public class CapacityTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Of_WhenNotPositive_Throws(int value)
    {
        var act = () => Capacity.Of(value);

        act.Should().Throw<DomainException>().WithMessage("*mayor que cero*");
    }

    [Fact]
    public void Of_WhenPositive_HoldsValue()
    {
        Capacity.Of(200).Value.Should().Be(200);
    }
}
