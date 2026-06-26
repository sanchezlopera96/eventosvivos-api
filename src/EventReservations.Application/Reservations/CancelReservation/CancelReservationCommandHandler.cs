using EventReservations.Application.Abstractions;
using EventReservations.Application.Common;
using EventReservations.Domain.Reservations;

namespace EventReservations.Application.Reservations.CancelReservation;

/// <summary>
/// RF-05: cancela una reserva confirmada. Según RN07 (decidido por el dominio),
/// libera el cupo o lo marca como perdido, aplicando el efecto en el evento.
/// </summary>
public sealed class CancelReservationCommandHandler
    : ICommandHandler<CancelReservationCommand, CancellationOutcome>
{
    private readonly IReservationRepository _reservations;
    private readonly IEventRepository _events;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public CancelReservationCommandHandler(
        IReservationRepository reservations,
        IEventRepository events,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _reservations = reservations;
        _events = events;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<CancellationOutcome> HandleAsync(
        CancelReservationCommand command, CancellationToken cancellationToken = default)
    {
        var reservation = await _reservations.GetByIdAsync(command.ReservationId, cancellationToken)
            ?? throw NotFoundException.For("reserva", command.ReservationId);

        var @event = await _events.GetByIdAsync(reservation.EventId, cancellationToken)
            ?? throw NotFoundException.For("evento", reservation.EventId);

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var outcome = reservation.Cancel(now, @event.Schedule.StartsAt);

        if (outcome == CancellationOutcome.SeatsReleased)
            @event.ReleaseSeats(reservation.Quantity);
        else
            @event.LoseSeats(reservation.Quantity); // RN07

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return outcome;
    }
}
