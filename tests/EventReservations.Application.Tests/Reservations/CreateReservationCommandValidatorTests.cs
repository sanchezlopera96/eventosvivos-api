using EventReservations.Application.Reservations.CreateReservation;
using FluentAssertions;
using Xunit;

namespace EventReservations.Application.Tests.Reservations;

public class CreateReservationCommandValidatorTests
{
    private readonly CreateReservationCommandValidator _validator = new();

    private static CreateReservationCommand Valid() =>
        new(Guid.NewGuid(), Quantity: 2, BuyerName: "Ana Pérez", BuyerEmail: "ana@correo.com");

    [Fact]
    public void Valid_PassesValidation()
        => _validator.Validate(Valid()).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Quantity_LessThanOne_Fails(int quantity)
        => _validator.Validate(Valid() with { Quantity = quantity }).IsValid.Should().BeFalse();

    [Fact]
    public void EventId_Empty_Fails()
        => _validator.Validate(Valid() with { EventId = Guid.Empty }).IsValid.Should().BeFalse();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BuyerName_Empty_Fails(string name)
        => _validator.Validate(Valid() with { BuyerName = name }).IsValid.Should().BeFalse();

    [Theory]
    [InlineData("")]
    [InlineData("no-es-email")]
    public void BuyerEmail_Invalid_Fails(string email)
        => _validator.Validate(Valid() with { BuyerEmail = email }).IsValid.Should().BeFalse();
}
