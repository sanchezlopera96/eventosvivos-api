using EventReservations.Application.Abstractions;
using EventReservations.Application.Common;

namespace EventReservations.Application.Reservations.ConfirmPayment;

/// <summary>
/// RF-04: confirma el pago de una reserva. El dominio genera el código EV-######;
/// su unicidad se garantiza con un índice único en la base de datos (ADR-004).
/// Ante una colisión (improbable, pero posible con 6 dígitos), se regenera el
/// código y se reintenta hasta un número acotado de veces.
/// </summary>
public sealed class ConfirmPaymentCommandHandler : ICommandHandler<ConfirmPaymentCommand, string>
{
    // Número máximo de intentos ante colisión del código. Con ~1M de combinaciones
    // la probabilidad de varias colisiones seguidas es despreciable.
    private const int MaxAttempts = 5;

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

        // Primer intento: confirma (genera el código) y persiste.
        reservation.ConfirmPayment(now);

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return reservation.Code!.Value;
            }
            catch (DuplicateReservationCodeException) when (attempt < MaxAttempts)
            {
                // Colisión del código: genera uno nuevo y reintenta.
                reservation.RegenerateCode();
            }
        }
    }
}
