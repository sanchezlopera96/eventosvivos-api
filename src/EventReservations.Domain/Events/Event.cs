using EventReservations.Domain.Common;
using EventReservations.Domain.Reservations;
using EventReservations.Domain.Venues;

namespace EventReservations.Domain.Events;

/// <summary>
/// Agregado raíz Evento. Concentra las reglas de creación (RF-01) y de ciclo de
/// vida (RN03, RN06), y es la FRONTERA DE CONSISTENCIA del aforo.
///
/// Aforo (ADR-004): una reserva en pendiente_pago ya bloquea cupo.
///   SeatsTaken  = plazas de reservas pendientes + confirmadas (bloquean venta)
///   LostSeats   = plazas perdidas por penalización RN07 (no se liberan)
///   AvailableSeats = Capacity - SeatsTaken - LostSeats
///
/// Las reglas de límite por transacción (RN04 &lt;1h, RN05 precio&gt;$100, RF-03
/// &lt;24h) se validan en la capa de aplicación, donde se dispone del contexto y
/// del reloj; el agregado garantiza la invariante de capacidad. La concurrencia
/// se protege con el token xmin en infraestructura (ADR-006).
/// </summary>
public sealed class Event : Entity<Guid>
{
    public const int TitleMinLength = 5;
    public const int TitleMaxLength = 100;
    public const int DescriptionMinLength = 10;
    public const int DescriptionMaxLength = 500;

    private static readonly TimeSpan WeekendCurfew = new(22, 0, 0);

    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public int VenueId { get; private set; }
    public Capacity Capacity { get; private set; } = null!;
    public Schedule Schedule { get; private set; } = null!;
    public Money Price { get; private set; } = null!;
    public EventType Type { get; private set; }
    public EventStatus Status { get; private set; }

    public int SeatsTaken { get; private set; }
    public int LostSeats { get; private set; }

    /// <summary>Plazas disponibles para la venta. Calculado, no se persiste.</summary>
    public int AvailableSeats => Capacity.Value - SeatsTaken - LostSeats;

    private Event() { } // EF Core

    private Event(
        Guid id, string title, string description, int venueId,
        Capacity capacity, Schedule schedule, Money price, EventType type)
    {
        Id = id;
        Title = title;
        Description = description;
        VenueId = venueId;
        Capacity = capacity;
        Schedule = schedule;
        Price = price;
        Type = type;
        Status = EventStatus.Activo;
        SeatsTaken = 0;
        LostSeats = 0;
    }

    public static Event Create(
        string title, string description, Venue venue,
        Capacity capacity, Schedule schedule, Money price,
        EventType type, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(venue);

        ValidateTitle(title);
        ValidateDescription(description);

        // RN01: la capacidad del evento no puede exceder la del venue.
        if (capacity.Value > venue.Capacity)
            throw new DomainException(
                "La capacidad del evento no puede exceder la capacidad del venue.");

        // RF-01: la fecha de inicio debe ser futura.
        if (schedule.StartsAt <= now)
            throw new DomainException("La fecha de inicio del evento debe ser futura.");

        // RN03: en fin de semana, no se permite iniciar después de las 22:00.
        if (IsWeekend(schedule.StartsAt) && schedule.StartsAt.TimeOfDay > WeekendCurfew)
            throw new DomainException(
                "Los eventos en fin de semana no pueden iniciar después de las 22:00.");

        return new Event(
            Guid.NewGuid(), title.Trim(), description.Trim(),
            venue.Id, capacity, schedule, price, type);
    }

    /// <summary>
    /// RF-03: crea una reserva (pendiente_pago) bloqueando el cupo. Valida estado
    /// del evento, cantidad positiva y disponibilidad. Es el único punto que
    /// aumenta SeatsTaken, evitando que el contador y las reservas diverjan.
    /// </summary>
    public Reservation Reserve(BuyerInfo buyer, int quantity, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(buyer);

        if (Status != EventStatus.Activo)
            throw new DomainException("El evento no admite reservas en su estado actual.");

        if (quantity <= 0)
            throw new DomainException("La cantidad de entradas debe ser al menos 1.");

        if (quantity > AvailableSeats)
            throw new DomainException("No hay entradas disponibles suficientes para la reserva.");

        SeatsTaken += quantity;
        return Reservation.Create(Id, buyer, quantity, now);
    }

    /// <summary>Libera plazas al cancelar una reserva con 48h o más de antelación.</summary>
    public void ReleaseSeats(int quantity)
    {
        GuardQuantityAgainstTaken(quantity);
        SeatsTaken -= quantity;
    }

    /// <summary>
    /// RN07: marca plazas como perdidas al cancelar con menos de 48h. Salen de
    /// SeatsTaken pero NO vuelven a estar disponibles para la venta.
    /// </summary>
    public void LoseSeats(int quantity)
    {
        GuardQuantityAgainstTaken(quantity);
        SeatsTaken -= quantity;
        LostSeats += quantity;
    }

    public void Cancel()
    {
        if (Status != EventStatus.Activo)
            throw new DomainException("Solo se puede cancelar un evento activo.");

        Status = EventStatus.Cancelado;
    }

    /// <summary>RN06: completa el evento si la fecha actual supera su fin.</summary>
    public void MarkCompletedIfEnded(DateTime now)
    {
        if (Status == EventStatus.Activo && now > Schedule.EndsAt)
            Status = EventStatus.Completado;
    }

    private void GuardQuantityAgainstTaken(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("La cantidad debe ser mayor que cero.");

        if (quantity > SeatsTaken)
            throw new DomainException("No se pueden liberar/perder más plazas de las ocupadas.");
    }

    private static void ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title) ||
            title.Trim().Length is < TitleMinLength or > TitleMaxLength)
            throw new DomainException(
                $"El título debe tener entre {TitleMinLength} y {TitleMaxLength} caracteres.");
    }

    private static void ValidateDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description) ||
            description.Trim().Length is < DescriptionMinLength or > DescriptionMaxLength)
            throw new DomainException(
                $"La descripción debe tener entre {DescriptionMinLength} y {DescriptionMaxLength} caracteres.");
    }

    private static bool IsWeekend(DateTime date)
        => date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
}
