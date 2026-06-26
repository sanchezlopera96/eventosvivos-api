using EventReservations.Application.Abstractions;
using EventReservations.Application.Common;
using EventReservations.Application.Reservations.CreateReservation;
using EventReservations.Application.Tests.Common;
using EventReservations.Domain.Common;
using EventReservations.Domain.Events;
using EventReservations.Domain.Reservations;
using EventReservations.Domain.Venues;
using FluentAssertions;
using Moq;
using Xunit;

namespace EventReservations.Application.Tests.Reservations;

public class CreateReservationCommandHandlerTests
{
    private static readonly DateTime EventStart = new(2026, 3, 11, 18, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IEventRepository> _events = new();
    private readonly Mock<IReservationRepository> _reservations = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private static Event BuildEvent(decimal price = 50m, int capacity = 100)
    {
        var venue = Venue.Create(1, "Auditorio Central", 200, "Bogotá");
        var schedule = Schedule.Create(EventStart, EventStart.AddHours(2));
        return Event.Create(
            "Concierto de Jazz", "Una noche de jazz en vivo con artistas invitados.",
            venue, Capacity.Of(capacity), schedule, Money.Of(price),
            EventType.Concierto, new DateTime(2026, 2, 1, 9, 0, 0, DateTimeKind.Utc));
    }

    private CreateReservationCommandHandler Handler(DateTime now) =>
        new(_events.Object, _reservations.Object, _uow.Object, new FixedTimeProvider(now));

    private static CreateReservationCommand Command(int quantity = 2) =>
        new(Guid.NewGuid(), quantity, "Ana Pérez", "ana@correo.com");

    private void SetupEvent(Event @event) =>
        _events.Setup(e => e.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(@event);

    [Fact]
    public async Task Handle_WhenEventNotFound_ThrowsNotFound()
    {
        _events.Setup(e => e.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync((Event?)null);

        var act = () => Handler(EventStart.AddHours(-48)).HandleAsync(Command());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenLessThanOneHour_ThrowsDomainException() // RN04
    {
        SetupEvent(BuildEvent());

        var act = () => Handler(EventStart.AddMinutes(-30)).HandleAsync(Command());

        await act.Should().ThrowAsync<DomainException>().WithMessage("*1 hora*");
    }

    [Fact]
    public async Task Handle_WhenLessThan24h_LimitIs5_Throws() // RF-03
    {
        SetupEvent(BuildEvent(price: 50m));

        var act = () => Handler(EventStart.AddHours(-12)).HandleAsync(Command(quantity: 6));

        await act.Should().ThrowAsync<DomainException>().WithMessage("*5*");
    }

    [Fact]
    public async Task Handle_WhenExpensiveAndLessThan24h_AppliesFive_NotTen() // RF-03 > RN05
    {
        SetupEvent(BuildEvent(price: 200m));

        // 6 entradas: superaria el limite RF-03 (5) aunque RN05 permitiria 10.
        var act = () => Handler(EventStart.AddHours(-12)).HandleAsync(Command(quantity: 6));

        await act.Should().ThrowAsync<DomainException>().WithMessage("*5*");
    }

    [Fact]
    public async Task Handle_WhenExpensiveAndFar_AllowsUpToTen() // RN05
    {
        SetupEvent(BuildEvent(price: 200m, capacity: 100));

        var id = await Handler(EventStart.AddHours(-48)).HandleAsync(Command(quantity: 10));

        id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_WhenValid_AddsReservationAndSaves()
    {
        SetupEvent(BuildEvent());

        var id = await Handler(EventStart.AddHours(-48)).HandleAsync(Command(quantity: 2));

        id.Should().NotBeEmpty();
        _reservations.Verify(r => r.AddAsync(It.IsAny<Reservation>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
