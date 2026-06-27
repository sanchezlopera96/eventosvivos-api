using EventReservations.Domain.Events;

namespace EventReservations.Application.Events.ListEvents;

/// <summary>
/// Consulta de listado de eventos con filtros opcionales (RF-02). Un filtro en
/// null significa "no filtrar por ese criterio".
/// </summary>
public sealed record ListEventsQuery(
    EventType? Type = null,
    int? VenueId = null,
    EventStatus? Status = null,
    DateTime? StartsFrom = null,
    DateTime? StartsTo = null,
    string? TitleContains = null);
