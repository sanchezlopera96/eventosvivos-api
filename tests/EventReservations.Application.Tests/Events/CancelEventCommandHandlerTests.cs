using EventReservations.Domain.Common;
using EventReservations.Application.Abstractions;
using EventReservations.Application.Common;
using EventReservations.Application.Events.CancelEvent;
using EventReservations.Application.Tests.Common;
using EventReservations.Domain.Events;
using EventReservations.Domain.Reservations;
using EventReservations.Domain.Venues;
using FluentAssertions;
using Moq;
using Xunit;

namespace EventReservations.Application.Tests.Events;

public class CancelEventCommandHandlerTests
{
    private static readonly DateTime Now = new(2026, 2, 1, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime EventStart = new(2026, 3, 11, 18, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IEventRepository> _events = new();
    private readonly Mock<IReservationRepository> _reservations = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private CancelEventCommandHandler Handler() =>
        new(_events.Object, _reservations.Object, _uow.Object, new FixedTimeProvider(Now));

    private static Event BuildEvent(out Reservation r1, out Reservation r2)
    {
        var venue = Venue.Create(1, "Auditorio Central", 200, "Bogotá");
        var schedule = Schedule.Create(EventStart, EventStart.AddHours(2));
        var @event = Event.Create(
            "Concierto de Jazz", "Una noche de jazz en vivo con artistas invitados.",
            venue, Capacity.Of(100), schedule, Money.Of(50m), EventType.Concierto, Now);
        r1 = @event.Reserve(BuyerInfo.Create("Ana", "ana@correo.com"), 2, Now);
        r2 = @event.Reserve(BuyerInfo.Create("Beto", "beto@correo.com"), 3, Now);
        r2.ConfirmPayment(Now);
        return @event;
    }

    [Fact]
    public async Task Handle_WhenEventNotFound_ThrowsNotFound()
    {
        _events.Setup(e => e.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync((Event?)null);

        var act = () => Handler().HandleAsync(new CancelEventCommand(Guid.NewGuid()));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenValid_CancelsEventAndCascadesReservations()
    {
        var @event = BuildEvent(out var r1, out var r2);
        _events.Setup(e => e.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(@event);
        _reservations.Setup(r => r.ListActiveByEventAsync(@event.Id, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(new[] { r1, r2 });

        await Handler().HandleAsync(new CancelEventCommand(@event.Id));

        @event.Status.Should().Be(EventStatus.Cancelado);
        r1.Status.Should().Be(ReservationStatus.Cancelada);
        r2.Status.Should().Be(ReservationStatus.Cancelada);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

