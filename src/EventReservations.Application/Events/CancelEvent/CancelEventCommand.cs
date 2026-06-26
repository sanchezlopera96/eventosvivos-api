namespace EventReservations.Application.Events.CancelEvent;

/// <summary>Comando para cancelar un evento (cancela en cascada sus reservas).</summary>
public sealed record CancelEventCommand(Guid EventId);
