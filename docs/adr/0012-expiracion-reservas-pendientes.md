# ADR-012: Expiración (TTL) de reservas pendientes de pago

- **Estado:** Aceptado (decisión documentada; implementación futura)
- **Fecha:** 2026-06

## Contexto

Por [ADR-004](0004-modelo-de-aforo.md), una reserva en estado `pendiente_pago` ya bloquea cupo: descuenta de `AvailableSeats` desde el momento de su creación, para impedir el *overbooking*. Por otro lado, RF-05 (correctamente, según el enunciado) solo permite cancelar reservas **confirmadas**: una reserva pendiente no puede cancelarse.

La combinación de ambas reglas tiene un efecto no deseado: una reserva pendiente que nunca se paga (un "carrito abandonado") mantiene su cupo bloqueado **indefinidamente**. Como RN06 solo afecta al estado del evento (no a las reservas) y no existe ningún proceso que libere pendientes vencidas, esas plazas no vuelven a la venta aunque el comprador nunca complete el pago.

No es una violación del enunciado —que obliga a rechazar la cancelación de pendientes—, pero sí es un caso borde relevante: el documento indica que los casos borde pesan tanto como el flujo feliz.

## Decisión

Se reconoce la limitación y se define la solución, dejando la implementación como evolución futura por estar fuera del alcance temporal de la prueba:

**Expiración por TTL de las reservas pendientes.** Una reserva `pendiente_pago` que supere un tiempo de vida máximo (p. ej. 15–30 minutos) se considera expirada: se cancela automáticamente y sus plazas se liberan (`Event.ReleaseSeats`), volviendo a estar disponibles.

Opciones de implementación consideradas:

1. **Job en segundo plano (`IHostedService`)** que periódicamente busca reservas pendientes vencidas (`CreatedAt + TTL < now`), las marca como canceladas y libera sus plazas. Es la opción más sencilla y desacoplada.
2. **Expiración perezosa en lectura**: al consultar la disponibilidad de un evento, descontar solo las reservas pendientes aún vigentes. Evita un job, pero ensucia las consultas y no persiste la liberación.
3. **Cola con expiración** (p. ej. mensajes diferidos): potente pero desproporcionado para esta prueba.

La opción preferida es la **1 (job en segundo plano)** por su simplicidad y porque mantiene la base de datos consistente.

## Consecuencias

- Mientras no se implemente, una reserva pendiente bloquea cupo hasta que se confirme o se cancele el evento completo. En la práctica el impacto es bajo (los administradores confirman pagos y el reporte ahora expone las plazas pendientes; ver [ADR/hallazgo de reporte](#)), pero conviene tenerlo presente.
- El reporte de ocupación expone explícitamente las **plazas pendientes (bloqueadas)**, de modo que el hueco entre disponibilidad y ocupación es visible y no "desaparece" sin explicación.
- La introducción del TTL requeriría: un campo de expiración o el uso de `CreatedAt`, un `IHostedService`, y un test de integración que verifique que una pendiente vencida libera su cupo.
- Define un comportamiento esperable para el usuario: una reserva sin pagar no retiene la entrada para siempre.
