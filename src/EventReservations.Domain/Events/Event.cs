using EventReservations.Domain.Common;
using EventReservations.Domain.Venues;

namespace EventReservations.Domain.Events;

/// <summary>
/// Agregado raíz Evento. Concentra las reglas de creación (RF-01) y de ciclo de
/// vida (RN03, RN06) y es la frontera de consistencia del aforo (las mecánicas
/// de reserva se añaden junto al agregado Reservation).
///
/// Las operaciones dependientes del tiempo reciben 'now' como parámetro para ser
/// deterministas y testeables.
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

    /// <summary>Cancela el evento. Solo posible desde estado Activo.</summary>
    public void Cancel()
    {
        if (Status != EventStatus.Activo)
            throw new DomainException("Solo se puede cancelar un evento activo.");

        Status = EventStatus.Cancelado;
    }

    /// <summary>
    /// RN06: marca el evento como completado si la fecha actual supera su fin.
    /// Idempotente y solo aplica a eventos activos (un evento cancelado no se
    /// completa).
    /// </summary>
    public void MarkCompletedIfEnded(DateTime now)
    {
        if (Status == EventStatus.Activo && now > Schedule.EndsAt)
            Status = EventStatus.Completado;
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
