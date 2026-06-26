using EventReservations.Application.Abstractions;
using EventReservations.Application.Common;
using EventReservations.Application.Events.CreateEvent;
using EventReservations.Application.Tests.Common;
using EventReservations.Domain.Common;
using EventReservations.Domain.Events;
using EventReservations.Domain.Venues;
using FluentAssertions;
using Moq;
using Xunit;

namespace EventReservations.Application.Tests.Events;

public class CreateEventCommandHandlerTests
{
    private static readonly DateTime Now = new(2026, 3, 1, 9, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IEventRepository> _events = new();
    private readonly Mock<IVenueRepository> _venues = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private CreateEventCommandHandler CreateHandler() =>
        new(_events.Object, _venues.Object, _uow.Object, new FixedTimeProvider(Now));

    private static CreateEventCommand ValidCommand(int capacity = 100) => new(
        Title: "Concierto de Jazz",
        Description: "Una noche de jazz en vivo con artistas invitados.",
        VenueId: 1,
        Capacity: capacity,
        StartsAt: new DateTime(2026, 3, 11, 18, 0, 0, DateTimeKind.Utc),
        EndsAt: new DateTime(2026, 3, 11, 20, 0, 0, DateTimeKind.Utc),
        Price: 50m,
        Type: EventType.Concierto);

    private void SetupVenue(int capacity = 200) =>
        _venues.Setup(v => v.GetByIdAsync(1, It.IsAny<CancellationToken>()))
               .ReturnsAsync(Venue.Create(1, "Auditorio Central", capacity, "Bogotá"));

    [Fact]
    public async Task Handle_WhenVenueNotFound_ThrowsNotFound()
    {
        _venues.Setup(v => v.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync((Venue?)null);

        var act = () => CreateHandler().HandleAsync(ValidCommand());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenVenueHasOverlap_ThrowsDomainException()
    {
        SetupVenue();
        _events.Setup(e => e.ExistsActiveOverlapAsync(
                    1, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(true); // RN02

        var act = () => CreateHandler().HandleAsync(ValidCommand());

        await act.Should().ThrowAsync<DomainException>().WithMessage("*superpuesto*");
        _events.Verify(e => e.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenCapacityExceedsVenue_ThrowsDomainException()
    {
        SetupVenue(capacity: 50);          // venue pequeño
        _events.Setup(e => e.ExistsActiveOverlapAsync(
                    It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(false);

        var act = () => CreateHandler().HandleAsync(ValidCommand(capacity: 100)); // > venue (RN01)

        await act.Should().ThrowAsync<DomainException>().WithMessage("*venue*");
    }

    [Fact]
    public async Task Handle_WhenValid_AddsEventAndSaves()
    {
        SetupVenue();
        _events.Setup(e => e.ExistsActiveOverlapAsync(
                    It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(false);

        var id = await CreateHandler().HandleAsync(ValidCommand());

        id.Should().NotBeEmpty();
        _events.Verify(e => e.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
