using EventReservations.Application.Abstractions;
using EventReservations.Application.Common;
using EventReservations.Application.Reservations.CancelReservation;
using EventReservations.Application.Tests.Common;
using EventReservations.Domain.Events;
using EventReservations.Domain.Reservations;
using EventReservations.Domain.Venues;
using FluentAssertions;
using Moq;
using Xunit;

namespace EventReservations.Application.Tests.Reservations;

public class CancelReservationCommandHandlerTests
{
    private static readonly DateTime CreationNow = new(2026, 2, 1, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime EventStart = new(2026, 3, 11, 18, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IReservationRepository> _reservations = new();
    private readonly Mock<IEventRepository> _events = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private CancelReservationCommandHandler Handler(DateTime now) =>
        new(_reservations.Object, _events.Object, _uow.Object, new FixedTimeProvider(now));

    // Crea un evento con una reserva confirmada de 4 plazas y los deja enlazados
    // en los mocks. Devuelve ambos para poder afirmar sobre el aforo.
    private (Event ev, Reservation res) SetupConfirmed()
    {
        var venue = Venue.Create(1, "Auditorio Central", 200, "Bogotá");
        var schedule = Schedule.Create(EventStart, EventStart.AddHours(2));
        var @event = Event.Create(
            "Concierto de Jazz", "Una noche de jazz en vivo con artistas invitados.",
            venue, Capacity.Of(100), schedule, Money.Of(50m), EventType.Concierto, CreationNow);
        var reservation = @event.Reserve(BuyerInfo.Create("Ana Pérez", "ana@correo.com"), 4, CreationNow);
        reservation.ConfirmPayment(CreationNow);

        _reservations.Setup(r => r.GetByIdAsync(reservation.Id, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(reservation);
        _events.Setup(e => e.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(@event);
        return (@event, reservation);
    }

    [Fact]
    public async Task Handle_WhenReservationNotFound_ThrowsNotFound()
    {
        _reservations.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync((Reservation?)null);

        var act = () => Handler(CreationNow).HandleAsync(new CancelReservationCommand(Guid.NewGuid()));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenMoreThan48h_ReleasesSeats()
    {
        var (ev, res) = SetupConfirmed();
        ev.SeatsTaken.Should().Be(4);

        await Handler(EventStart.AddHours(-72)).HandleAsync(new CancelReservationCommand(res.Id));

        res.Status.Should().Be(ReservationStatus.Cancelada);
        ev.SeatsTaken.Should().Be(0);
        ev.LostSeats.Should().Be(0);
        ev.AvailableSeats.Should().Be(100);
    }

    [Fact]
    public async Task Handle_WhenLessThan48h_ForfeitsSeats() // RN07
    {
        var (ev, res) = SetupConfirmed();

        await Handler(EventStart.AddHours(-24)).HandleAsync(new CancelReservationCommand(res.Id));

        res.Status.Should().Be(ReservationStatus.Cancelada);
        ev.SeatsTaken.Should().Be(0);
        ev.LostSeats.Should().Be(4);          // perdidas, no se liberan
        ev.AvailableSeats.Should().Be(96);
    }
}
