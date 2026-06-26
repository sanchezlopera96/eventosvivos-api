using EventReservations.Domain.Common;
using EventReservations.Domain.Venues;
using FluentAssertions;
using Xunit;

namespace EventReservations.Domain.Tests.Venues;

public class VenueTests
{
    [Fact]
    public void Create_WhenValid_HoldsValues()
    {
        var venue = Venue.Create(1, "Auditorio Central", 200, "Bogotá");

        venue.Id.Should().Be(1);
        venue.Name.Should().Be("Auditorio Central");
        venue.Capacity.Should().Be(200);
        venue.City.Should().Be("Bogotá");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Create_WhenCapacityNotPositive_Throws(int capacity)
    {
        var act = () => Venue.Create(1, "Auditorio Central", capacity, "Bogotá");

        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WhenNameEmpty_Throws(string? name)
    {
        var act = () => Venue.Create(1, name!, 200, "Bogotá");

        act.Should().Throw<DomainException>();
    }
}
