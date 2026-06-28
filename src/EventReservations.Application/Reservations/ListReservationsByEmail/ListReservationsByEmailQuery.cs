using EventReservations.Application.Reservations.ListReservations;

namespace EventReservations.Application.Reservations.ListReservationsByEmail;

// Busca las reservas asociadas a un correo. Reutiliza ReservationListItemDto.
//
// NOTA DE DISENO (privacidad): este endpoint es publico y asume el correo como
// identificador del comprador. En un sistema real deberia exigir verificacion
// de identidad (login del comprador o codigo de un solo uso enviado por email)
// para no exponer las reservas de un correo a cualquiera que lo escriba. Se
// asume asi por alcance de la prueba; ver ADR correspondiente.
public sealed record ListReservationsByEmailQuery(string Email);
