using EventReservations.Domain.Events;

namespace EventReservations.Application.Events.UpdateEvent;

/// <summary>
/// Comando para editar un evento (solo activos). Lleva el Id del evento y los
/// nuevos datos en tipos primitivos; el dominio reconstruye los value objects y
/// valida las invariantes (RN01, RN03, fecha futura, capacidad >= ocupadas).
/// </summary>
public sealed record UpdateEventCommand(
    Guid EventId,
    string Title,
    string Description,
    int VenueId,
    int Capacity,
    DateTime StartsAt,
    DateTime EndsAt,
    decimal Price,
    EventType Type);
