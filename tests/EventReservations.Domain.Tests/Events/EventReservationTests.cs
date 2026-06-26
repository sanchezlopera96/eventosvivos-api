using EventReservations.Domain.Common;
using EventReservations.Domain.Events;
using EventReservations.Domain.Reservations;
using EventReservations.Domain.Venues;
using FluentAssertions;
using Xunit;

namespace EventReservations.Domain.Tests.Events;

public class EventReservationTests
{
    private static readonly DateTime Now = new(2026, 3, 1, 9, 0, 0, DateTimeKind.Utc);
    private static readonly Venue Auditorio = Venue.Create(1, "Auditorio Central", 200, "Bogotá");

    private static Event ActiveEvent(int capacity = 100)
    {
        var schedule = Schedule.Create(
            new DateTime(2026, 3, 11, 18, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 3, 11, 20, 0, 0, DateTimeKind.Utc));

        return Event.Create(
            "Concierto de Jazz", "Una noche de jazz en vivo con artistas invitados.",
            Auditorio, Capacity.Of(capacity), schedule, Money.Of(50m),
            EventType.Concierto, Now);
    }

    private static BuyerInfo Buyer() => BuyerInfo.Create("Ana Pérez", "ana@correo.com");

    // ---------- Reserve ----------

    [Fact]
    public void Reserve_WhenActiveAndAvailable_ReturnsPendingReservationAndBlocksSeats()
    {
        var @event = ActiveEvent(capacity: 100);

        var reservation = @event.Reserve(Buyer(), quantity: 3, Now);

        reservation.Status.Should().Be(ReservationStatus.PendientePago);
        reservation.EventId.Should().Be(@event.Id);
        reservation.Quantity.Should().Be(3);
        @event.SeatsTaken.Should().Be(3);
        @event.AvailableSeats.Should().Be(97); // pendiente_pago ya bloquea cupo (ADR-004)
    }

    [Fact]
    public void Reserve_ExactlyAllSeats_Succeeds()
    {
        var @event = ActiveEvent(capacity: 10);

        @event.Reserve(Buyer(), quantity: 10, Now);

        @event.AvailableSeats.Should().Be(0);
    }

    [Fact]
    public void Reserve_WhenQuantityExceedsAvailable_Throws()
    {
        var @event = ActiveEvent(capacity: 5);

        var act = () => @event.Reserve(Buyer(), quantity: 6, Now);

        act.Should().Throw<DomainException>().WithMessage("*disponibles*");
        @event.SeatsTaken.Should().Be(0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void Reserve_WhenQuantityNotPositive_Throws(int quantity)
    {
        var @event = ActiveEvent();

        var act = () => @event.Reserve(Buyer(), quantity, Now);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Reserve_WhenEventNotActive_Throws()
    {
        var @event = ActiveEvent();
        @event.Cancel();

        var act = () => @event.Reserve(Buyer(), 1, Now);

        act.Should().Throw<DomainException>();
    }

    // ---------- ReleaseSeats (cancelación con >= 48h) ----------

    [Fact]
    public void ReleaseSeats_FreesAvailability()
    {
        var @event = ActiveEvent(capacity: 100);
        @event.Reserve(Buyer(), quantity: 4, Now);

        @event.ReleaseSeats(4);

        @event.SeatsTaken.Should().Be(0);
        @event.AvailableSeats.Should().Be(100);
        @event.LostSeats.Should().Be(0);
    }

    // ---------- LoseSeats (RN07: cancelación con < 48h) ----------

    [Fact]
    public void LoseSeats_KeepsSeatsBlockedAndMovesToLost()
    {
        var @event = ActiveEvent(capacity: 100);
        @event.Reserve(Buyer(), quantity: 5, Now);

        @event.LoseSeats(5);

        @event.SeatsTaken.Should().Be(0);
        @event.LostSeats.Should().Be(5);
        @event.AvailableSeats.Should().Be(95); // perdidas NO se liberan para venta
    }
}
