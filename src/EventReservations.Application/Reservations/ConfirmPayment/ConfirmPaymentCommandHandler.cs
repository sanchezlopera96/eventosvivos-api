using EventReservations.Application.Abstractions;
using EventReservations.Application.Common;

namespace EventReservations.Application.Reservations.ConfirmPayment;

/// <summary>
/// RF-04: confirma el pago de una reserva. El dominio genera el código EV-######;
/// su unicidad se garantiza con un índice único en la base de datos (ADR-004).
/// </summary>
public sealed class ConfirmPaymentCommandHandler : ICommandHandler<ConfirmPaymentCommand, string>
{
    private readonly IReservationRepository _reservations;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public ConfirmPaymentCommandHandler(
        IReservationRepository reservations,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _reservations = reservations;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<string> HandleAsync(ConfirmPaymentCommand command, CancellationToken cancellationToken = default)
    {
        var reservation = await _reservations.GetByIdAsync(command.ReservationId, cancellationToken)
            ?? throw NotFoundException.For("reserva", command.ReservationId);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        reservation.ConfirmPayment(now);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return reservation.Code!.Value;
    }
}
