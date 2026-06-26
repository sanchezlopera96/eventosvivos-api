using EventReservations.Application.Reservations;
using FluentAssertions;
using Xunit;

namespace EventReservations.Application.Tests.Reservations;

public class ReservationPolicyTests
{
    private static readonly DateTime Start = new(2026, 3, 11, 18, 0, 0, DateTimeKind.Utc);

    // ---------- RN04: antelación mínima de 1 hora ----------

    [Fact]
    public void ReservationsAllowed_WhenMoreThanOneHour_True()
        => ReservationPolicy.ReservationsAllowed(Start, Start.AddHours(-2)).Should().BeTrue();

    [Fact]
    public void ReservationsAllowed_ExactlyOneHour_True()
        => ReservationPolicy.ReservationsAllowed(Start, Start.AddHours(-1)).Should().BeTrue();

    [Fact]
    public void ReservationsAllowed_LessThanOneHour_False()
        => ReservationPolicy.ReservationsAllowed(Start, Start.AddMinutes(-30)).Should().BeFalse();

    // ---------- Límite por transacción ----------

    [Fact]
    public void MaxTickets_WhenFarAndCheap_NoLimit()
    {
        var now = Start.AddHours(-48); // >=24h
        ReservationPolicy.MaxTicketsPerTransaction(price: 50m, Start, now).Should().BeNull();
    }

    [Fact]
    public void MaxTickets_WhenFarAndExpensive_Rn05LimitsTo10()
    {
        var now = Start.AddHours(-48); // >=24h
        ReservationPolicy.MaxTicketsPerTransaction(price: 200m, Start, now).Should().Be(10);
    }

    [Fact]
    public void MaxTickets_WhenLessThan24hAndCheap_Rf03LimitsTo5()
    {
        var now = Start.AddHours(-12); // <24h
        ReservationPolicy.MaxTicketsPerTransaction(price: 50m, Start, now).Should().Be(5);
    }

    [Fact]
    public void MaxTickets_WhenLessThan24hAndExpensive_Rf03TakesPriorityOverRn05()
    {
        // Caso borde estrella: evento caro Y a menos de 24h.
        // RF-03 (máx 5) tiene prioridad sobre RN05 (máx 10).
        var now = Start.AddHours(-12);
        ReservationPolicy.MaxTicketsPerTransaction(price: 200m, Start, now).Should().Be(5);
    }

    [Fact]
    public void MaxTickets_Exactly24h_NotLateWindow()
    {
        var now = Start.AddHours(-24); // frontera: "menos de 24h" es estricto
        ReservationPolicy.MaxTicketsPerTransaction(price: 50m, Start, now).Should().BeNull();
    }

    [Fact]
    public void MaxTickets_PriceExactly100_NotHighPrice()
    {
        var now = Start.AddHours(-48); // >=24h
        ReservationPolicy.MaxTicketsPerTransaction(price: 100m, Start, now).Should().BeNull();
    }
}
