using EventReservations.Application.Abstractions;
using EventReservations.Application.Common;

namespace EventReservations.Application.Events.CancelEvent;

/// <summary>
/// Cancela un evento y, en cascada, sus reservas activas. La cancelación por
/// evento NO penaliza (la penalización RN07 es solo para cancelaciones del
/// comprador). Devuelve el número de reservas canceladas.
/// </summary>
public sealed class CancelEventCommandHandler : ICommandHandler<CancelEventCommand, int>
{
    private readonly IEventRepository _events;
    private readonly IReservationRepository _reservations;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public CancelEventCommandHandler(
        IEventRepository events,
        IReservationRepository reservations,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _events = events;
        _reservations = reservations;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<int> HandleAsync(CancelEventCommand command, CancellationToken cancellationToken = default)
    {
        var @event = await _events.GetByIdAsync(command.EventId, cancellationToken)
            ?? throw NotFoundException.For("evento", command.EventId);

        @event.Cancel();

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var reservations = await _reservations.ListActiveByEventAsync(@event.Id, cancellationToken);

        foreach (var reservation in reservations)
            reservation.CancelDueToEventCancellation(now);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return reservations.Count;
    }
}
