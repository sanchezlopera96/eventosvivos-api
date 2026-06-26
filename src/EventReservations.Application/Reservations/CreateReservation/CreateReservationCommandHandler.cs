using EventReservations.Application.Abstractions;
using EventReservations.Application.Common;
using EventReservations.Domain.Common;
using EventReservations.Domain.Reservations;

namespace EventReservations.Application.Reservations.CreateReservation;

/// <summary>
/// Crea una reserva (pendiente_pago). Aplica las reglas de reserva por
/// transacción (RN04, RN05, RF-03) y delega en Event.Reserve la invariante de
/// aforo y el estado del evento.
/// </summary>
public sealed class CreateReservationCommandHandler
    : ICommandHandler<CreateReservationCommand, Guid>
{
    private readonly IEventRepository _events;
    private readonly IReservationRepository _reservations;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public CreateReservationCommandHandler(
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

    public async Task<Guid> HandleAsync(CreateReservationCommand command, CancellationToken cancellationToken = default)
    {
        var @event = await _events.GetByIdAsync(command.EventId, cancellationToken)
            ?? throw NotFoundException.For("evento", command.EventId);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var startsAt = @event.Schedule.StartsAt;

        // RN04: antelación mínima de 1 hora.
        if (!ReservationPolicy.ReservationsAllowed(startsAt, now))
            throw new DomainException(
                "No se permiten reservas para eventos que inicien en menos de 1 hora.");

        // RN05 / RF-03: límite de entradas por transacción (RF-03 tiene prioridad).
        var maxPerTransaction = ReservationPolicy.MaxTicketsPerTransaction(
            @event.Price.Amount, startsAt, now);

        if (maxPerTransaction is { } limit && command.Quantity > limit)
            throw new DomainException(
                $"Solo se permiten {limit} entradas por transacción para este evento.");

        var buyer = BuyerInfo.Create(command.BuyerName, command.BuyerEmail);

        // Event.Reserve valida estado del evento, cantidad y disponibilidad,
        // y bloquea el cupo (ADR-004).
        var reservation = @event.Reserve(buyer, command.Quantity, now);

        await _reservations.AddAsync(reservation, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return reservation.Id;
    }
}
