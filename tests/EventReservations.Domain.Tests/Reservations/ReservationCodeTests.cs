using System.Text.RegularExpressions;
using EventReservations.Domain.Common;
using EventReservations.Domain.Reservations;
using FluentAssertions;
using Xunit;

namespace EventReservations.Domain.Tests.Reservations;

public class ReservationCodeTests
{
    [Fact]
    public void Generate_ProducesValidFormat()
    {
        var code = ReservationCode.Generate();

        Regex.IsMatch(code.Value, @"^EV-\d{6}$").Should().BeTrue();
    }

    [Theory]
    [InlineData("123456")]      // sin prefijo
    [InlineData("EV-123")]      // pocos dígitos
    [InlineData("EV-1234567")]  // demasiados dígitos
    [InlineData("ev-123456")]   // minúsculas
    [InlineData("EV-12A456")]   // carácter no numérico
    public void From_WhenInvalidFormat_Throws(string value)
    {
        var act = () => ReservationCode.From(value);

        act.Should().Throw<DomainException>().WithMessage("*formato*");
    }

    [Fact]
    public void From_WhenValid_HoldsValue()
    {
        ReservationCode.From("EV-000042").Value.Should().Be("EV-000042");
    }
}
