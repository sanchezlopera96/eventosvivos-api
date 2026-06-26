using EventReservations.Domain.Events;

namespace EventReservations.Application.Events.CreateEvent;

/// <summary>
/// Comando para crear un evento (RF-01). Lleva los datos de entrada en tipos
/// primitivos; el dominio se encarga de construir los value objects y validar
/// las invariantes de negocio (RN01, RN03, fecha futura).
/// </summary>
public sealed record CreateEventCommand(
    string Title,
    string Description,
    int VenueId,
    int Capacity,
    DateTime StartsAt,
    DateTime EndsAt,
    decimal Price,
    EventType Type);
