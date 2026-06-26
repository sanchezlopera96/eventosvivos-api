using EventReservations.Domain.Common;
using FluentAssertions;
using Xunit;

namespace EventReservations.Domain.Tests.Common;

public class MoneyTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-99.99)]
    public void Of_WhenNotPositive_Throws(decimal value)
    {
        var act = () => Money.Of(value);

        act.Should().Throw<DomainException>().WithMessage("*positivo*");
    }

    [Fact]
    public void Of_WhenPositive_HoldsValue()
    {
        Money.Of(150.50m).Amount.Should().Be(150.50m);
    }
}
