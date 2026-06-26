using System.Text.RegularExpressions;
using EventReservations.Domain.Common;
using EventReservations.Domain.Events;
using EventReservations.Domain.Reservations;
using EventReservations.Domain.Venues;
using FluentAssertions;
using Xunit;

namespace EventReservations.Domain.Tests.Reservations;

public class ReservationTests
{
    private static readonly DateTime CreationNow = new(2026, 3, 1, 9, 0, 0, DateTimeKind.Utc);
    private static readonly Venue Auditorio = Venue.Create(1, "Auditorio Central", 200, "Bogotá");

    // Evento que inicia el miércoles 2026-03-11 a las 18:00.
    private static readonly DateTime EventStart = new(2026, 3, 11, 18, 0, 0, DateTimeKind.Utc);

    private static Reservation NewPending()
    {
        var schedule = Schedule.Create(EventStart, EventStart.AddHours(2));
        var @event = Event.Create(
            "Concierto de Jazz", "Una noche de jazz en vivo con artistas invitados.",
            Auditorio, Capacity.Of(100), schedule, Money.Of(50m),
            EventType.Concierto, CreationNow);

        return @event.Reserve(BuyerInfo.Create("Ana Pérez", "ana@correo.com"), quantity: 2, CreationNow);
    }

    private static Reservation NewConfirmed()
    {
        var reservation = NewPending();
        reservation.ConfirmPayment(CreationNow);
        return reservation;
    }

    // ---------- Estado inicial (RF-03) ----------

    [Fact]
    public void Reserve_StartsAsPendingWithoutCode()
    {
        var reservation = NewPending();

        reservation.Status.Should().Be(ReservationStatus.PendientePago);
        reservation.Code.Should().BeNull();
    }

    // ---------- Confirmar pago (RF-04) ----------

    [Fact]
    public void ConfirmPayment_FromPending_ConfirmsAndGeneratesCode()
    {
        var reservation = NewPending();

        reservation.ConfirmPayment(CreationNow);

        reservation.Status.Should().Be(ReservationStatus.Confirmada);
        reservation.Code.Should().NotBeNull();
        Regex.IsMatch(reservation.Code!.Value, @"^EV-\d{6}$").Should().BeTrue();
    }

    [Fact]
    public void ConfirmPayment_WhenAlreadyConfirmed_Throws()
    {
        var reservation = NewConfirmed();

        var act = () => reservation.ConfirmPayment(CreationNow);

        act.Should().Throw<DomainException>().WithMessage("*confirmada*");
    }

    [Fact]
    public void ConfirmPayment_WhenCancelled_Throws()
    {
        var reservation = NewConfirmed();
        reservation.Cancel(CreationNow, EventStart); // >48h: se cancela bien

        var act = () => reservation.ConfirmPayment(CreationNow);

        act.Should().Throw<DomainException>();
    }

    // ---------- Cancelar (RF-05) ----------

    [Fact]
    public void Cancel_WhenPending_Throws()
    {
        var reservation = NewPending();

        var act = () => reservation.Cancel(CreationNow, EventStart);

        act.Should().Throw<DomainException>().WithMessage("*confirmada*");
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_Throws()
    {
        var reservation = NewConfirmed();
        reservation.Cancel(CreationNow, EventStart);

        var act = () => reservation.Cancel(CreationNow, EventStart);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Cancel_RecordsCancellationTime()
    {
        var reservation = NewConfirmed();
        var cancelTime = new DateTime(2026, 3, 5, 10, 0, 0, DateTimeKind.Utc);

        reservation.Cancel(cancelTime, EventStart);

        reservation.Status.Should().Be(ReservationStatus.Cancelada);
        reservation.CancelledAt.Should().Be(cancelTime);
    }

    // ---------- RN07: penalización por cancelación tardía (< 48h) ----------

    [Fact]
    public void Cancel_MoreThan48hBefore_ReturnsSeatsReleased()
    {
        var reservation = NewConfirmed();
        var now = EventStart.AddHours(-54); // bastante antes

        var outcome = reservation.Cancel(now, EventStart);

        outcome.Should().Be(CancellationOutcome.SeatsReleased);
    }

    [Fact]
    public void Cancel_Exactly48hBefore_ReturnsSeatsReleased()
    {
        var reservation = NewConfirmed();
        var now = EventStart.AddHours(-48); // frontera: "menos de 48h" es estricto

        var outcome = reservation.Cancel(now, EventStart);

        outcome.Should().Be(CancellationOutcome.SeatsReleased);
    }

    [Fact]
    public void Cancel_LessThan48hBefore_ReturnsSeatsForfeited()
    {
        var reservation = NewConfirmed();
        var now = EventStart.AddHours(-24); // dentro de la ventana de penalización

        var outcome = reservation.Cancel(now, EventStart);

        outcome.Should().Be(CancellationOutcome.SeatsForfeited);
    }
}
