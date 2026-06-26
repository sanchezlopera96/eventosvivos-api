using EventReservations.Application.Events.CreateEvent;
using EventReservations.Domain.Events;
using FluentAssertions;
using Xunit;

namespace EventReservations.Application.Tests.Events;

public class CreateEventCommandValidatorTests
{
    private readonly CreateEventCommandValidator _validator = new();

    private static CreateEventCommand Valid() => new(
        "Concierto de Jazz", "Una noche de jazz en vivo con artistas invitados.",
        VenueId: 1, Capacity: 100,
        StartsAt: new DateTime(2026, 3, 11, 18, 0, 0, DateTimeKind.Utc),
        EndsAt: new DateTime(2026, 3, 11, 20, 0, 0, DateTimeKind.Utc),
        Price: 50m, Type: EventType.Concierto);

    [Fact]
    public void Valid_PassesValidation()
    {
        _validator.Validate(Valid()).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("Jazz")]                 // < 5
    [InlineData("")]
    public void Title_OutOfRange_Fails(string title)
    {
        _validator.Validate(Valid() with { Title = title }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Description_TooShort_Fails()
    {
        _validator.Validate(Valid() with { Description = "corta" }).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Capacity_NotPositive_Fails(int capacity)
    {
        _validator.Validate(Valid() with { Capacity = capacity }).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Price_NotPositive_Fails(decimal price)
    {
        _validator.Validate(Valid() with { Price = price }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void EndsAt_NotAfterStartsAt_Fails()
    {
        var start = new DateTime(2026, 3, 11, 18, 0, 0, DateTimeKind.Utc);
        _validator.Validate(Valid() with { StartsAt = start, EndsAt = start }).IsValid.Should().BeFalse();
    }
}
