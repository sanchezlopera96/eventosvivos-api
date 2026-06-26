using EventReservations.Domain.Common;

namespace EventReservations.Domain.Reservations;

/// <summary>
/// Agregado raíz Reserva. Referencia al evento por Id (frontera de consistencia
/// propia). Solo puede crearse vía <c>Event.Reserve(...)</c> (fábrica internal),
/// garantizando que el aforo se valida antes de existir la reserva.
/// </summary>
public sealed class Reservation : Entity<Guid>
{
    /// <summary>Penalización por cancelación tardía (RN07): menos de 48 horas.</summary>
    private static readonly TimeSpan LateCancellationThreshold = TimeSpan.FromHours(48);

    public Guid EventId { get; private set; }
    public BuyerInfo Buyer { get; private set; } = null!;
    public int Quantity { get; private set; }
    public ReservationStatus Status { get; private set; }
    public ReservationCode? Code { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }

    private Reservation() { } // EF Core

    private Reservation(Guid id, Guid eventId, BuyerInfo buyer, int quantity, DateTime now)
    {
        Id = id;
        EventId = eventId;
        Buyer = buyer;
        Quantity = quantity;
        Status = ReservationStatus.PendientePago;
        CreatedAt = now;
    }

    internal static Reservation Create(Guid eventId, BuyerInfo buyer, int quantity, DateTime now)
        => new(Guid.NewGuid(), eventId, buyer, quantity, now);

    /// <summary>
    /// RF-04: confirma el pago. Genera el código EV-###### y pasa a Confirmada.
    /// La unicidad del código se garantiza en persistencia (índice único) con
    /// reintento ante colisión en la capa de aplicación.
    /// </summary>
    public void ConfirmPayment(DateTime now)
    {
        if (Status == ReservationStatus.Confirmada)
            throw new DomainException("La reserva ya está confirmada.");

        if (Status == ReservationStatus.Cancelada)
            throw new DomainException("No se puede confirmar una reserva cancelada.");

        Status = ReservationStatus.Confirmada;
        Code = ReservationCode.Generate();
        ConfirmedAt = now;
    }

    /// <summary>
    /// RF-05: cancela la reserva. Solo permitido desde Confirmada. Devuelve el
    /// efecto sobre el aforo según RN07: si faltan menos de 48h para el evento,
    /// las plazas se pierden; en caso contrario se liberan.
    /// </summary>
    public CancellationOutcome Cancel(DateTime now, DateTime eventStartsAt)
    {
        if (Status != ReservationStatus.Confirmada)
            throw new DomainException("Solo se pueden cancelar reservas confirmadas.");

        Status = ReservationStatus.Cancelada;
        CancelledAt = now;

        var timeUntilEvent = eventStartsAt - now;
        return timeUntilEvent < LateCancellationThreshold
            ? CancellationOutcome.SeatsForfeited
            : CancellationOutcome.SeatsReleased;
    }
}
