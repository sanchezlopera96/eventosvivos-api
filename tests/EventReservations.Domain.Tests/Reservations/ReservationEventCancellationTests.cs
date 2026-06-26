using EventReservations.Domain.Common;
using EventReservations.Domain.Events;
using EventReservations.Domain.Reservations;
using EventReservations.Domain.Venues;
using FluentAssertions;
using Xunit;

namespace EventReservations.Domain.Tests.Reservations;

public class ReservationEventCancellationTests
{
    private static readonly DateTime Now = new(2026, 3, 1, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime EventStart = new(2026, 3, 11, 18, 0, 0, DateTimeKind.Utc);

    private static Reservation Pending()
    {
        var venue = Venue.Create(1, "Auditorio Central", 200, "Bogotá");
        var schedule = Schedule.Create(EventStart, EventStart.AddHours(2));
        var @event = Event.Create(
            "Concierto de Jazz", "Una noche de jazz en vivo con artistas invitados.",
            venue, Capacity.Of(100), schedule, Money.Of(50m), EventType.Concierto, Now);
        return @event.Reserve(BuyerInfo.Create("Ana Pérez", "ana@correo.com"), 2, Now);
    }

    [Fact]
    public void CancelDueToEventCancellation_FromPending_SetsCancelled()
    {
        var reservation = Pending();

        reservation.CancelDueToEventCancellation(Now);

        reservation.Status.Should().Be(ReservationStatus.Cancelada);
        reservation.CancelledAt.Should().Be(Now);
    }

    [Fact]
    public void CancelDueToEventCancellation_FromConfirmed_SetsCancelled()
    {
        var reservation = Pending();
        reservation.ConfirmPayment(Now);

        reservation.CancelDueToEventCancellation(Now);

        reservation.Status.Should().Be(ReservationStatus.Cancelada);
    }

    [Fact]
    public void CancelDueToEventCancellation_WhenAlreadyCancelled_IsIdempotent()
    {
        var reservation = Pending();
        reservation.CancelDueToEventCancellation(Now);

        var act = () => reservation.CancelDueToEventCancellation(Now);

        act.Should().NotThrow();
        reservation.Status.Should().Be(ReservationStatus.Cancelada);
    }
}
