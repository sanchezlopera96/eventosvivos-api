using System.Text.RegularExpressions;
using EventReservations.Application.Abstractions;
using EventReservations.Application.Common;
using EventReservations.Application.Reservations.ConfirmPayment;
using EventReservations.Application.Tests.Common;
using EventReservations.Domain.Common;
using EventReservations.Domain.Events;
using EventReservations.Domain.Reservations;
using EventReservations.Domain.Venues;
using FluentAssertions;
using Moq;
using Xunit;

namespace EventReservations.Application.Tests.Reservations;

public class ConfirmPaymentCommandHandlerTests
{
    private static readonly DateTime Now = new(2026, 3, 1, 9, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IReservationRepository> _reservations = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private ConfirmPaymentCommandHandler Handler() =>
        new(_reservations.Object, _uow.Object, new FixedTimeProvider(Now));

    private static Reservation PendingReservation()
    {
        var venue = Venue.Create(1, "Auditorio Central", 200, "Bogotá");
        var schedule = Schedule.Create(
            new DateTime(2026, 3, 11, 18, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 3, 11, 20, 0, 0, DateTimeKind.Utc));
        var @event = Event.Create(
            "Concierto de Jazz", "Una noche de jazz en vivo con artistas invitados.",
            venue, Capacity.Of(100), schedule, Money.Of(50m), EventType.Concierto, Now);
        return @event.Reserve(BuyerInfo.Create("Ana Pérez", "ana@correo.com"), 2, Now);
    }

    [Fact]
    public async Task Handle_WhenReservationNotFound_ThrowsNotFound()
    {
        _reservations.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync((Reservation?)null);

        var act = () => Handler().HandleAsync(new ConfirmPaymentCommand(Guid.NewGuid()));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenPending_ConfirmsAndReturnsCode()
    {
        var reservation = PendingReservation();
        _reservations.Setup(r => r.GetByIdAsync(reservation.Id, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(reservation);

        var code = await Handler().HandleAsync(new ConfirmPaymentCommand(reservation.Id));

        reservation.Status.Should().Be(ReservationStatus.Confirmada);
        Regex.IsMatch(code, @"^EV-\d{6}$").Should().BeTrue();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenAlreadyConfirmed_ThrowsDomainException()
    {
        var reservation = PendingReservation();
        reservation.ConfirmPayment(Now);
        _reservations.Setup(r => r.GetByIdAsync(reservation.Id, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(reservation);

        var act = () => Handler().HandleAsync(new ConfirmPaymentCommand(reservation.Id));

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Handle_WhenCodeCollides_RegeneratesAndRetries()
    {
        var reservation = PendingReservation();
        _reservations.Setup(r => r.GetByIdAsync(reservation.Id, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(reservation);

        // El primer guardado colisiona (código duplicado); el segundo tiene éxito.
        var calls = 0;
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                calls++;
                if (calls == 1)
                    throw new DuplicateReservationCodeException(new Exception("23505"));
                return Task.FromResult(1);
            });

        var code = await Handler().HandleAsync(new ConfirmPaymentCommand(reservation.Id));

        // Reintentó: dos guardados, código final válido y reserva confirmada.
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        Regex.IsMatch(code, @"^EV-\d{6}$").Should().BeTrue();
        reservation.Status.Should().Be(ReservationStatus.Confirmada);
    }

    [Fact]
    public async Task Handle_WhenCollisionPersists_PropagatesAfterMaxAttempts()
    {
        var reservation = PendingReservation();
        _reservations.Setup(r => r.GetByIdAsync(reservation.Id, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(reservation);

        // Todos los intentos colisionan: la excepción debe propagarse al agotar
        // los reintentos (no se traga el error).
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DuplicateReservationCodeException(new Exception("23505")));

        var act = () => Handler().HandleAsync(new ConfirmPaymentCommand(reservation.Id));

        await act.Should().ThrowAsync<DuplicateReservationCodeException>();
        // Se intentó el número máximo de veces (5).
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(5));
    }
}
