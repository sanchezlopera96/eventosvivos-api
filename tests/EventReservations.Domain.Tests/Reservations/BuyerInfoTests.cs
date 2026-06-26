using EventReservations.Domain.Common;
using EventReservations.Domain.Reservations;
using FluentAssertions;
using Xunit;

namespace EventReservations.Domain.Tests.Reservations;

public class BuyerInfoTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WhenNameEmpty_Throws(string? name)
    {
        var act = () => BuyerInfo.Create(name!, "comprador@correo.com");

        act.Should().Throw<DomainException>().WithMessage("*nombre*");
    }

    [Theory]
    [InlineData("no-es-email")]
    [InlineData("falta@")]
    [InlineData("@dominio.com")]
    [InlineData("con espacio@correo.com")]
    [InlineData("sinpunto@correo")]
    public void Create_WhenEmailInvalid_Throws(string email)
    {
        var act = () => BuyerInfo.Create("Ana Pérez", email);

        act.Should().Throw<DomainException>().WithMessage("*email*");
    }

    [Fact]
    public void Create_WhenValid_NormalizesAndHolds()
    {
        var buyer = BuyerInfo.Create("  Ana Pérez  ", "  Ana@Correo.COM ");

        buyer.Name.Should().Be("Ana Pérez");       // recortado
        buyer.Email.Should().Be("ana@correo.com"); // recortado y en minúsculas
    }
}
