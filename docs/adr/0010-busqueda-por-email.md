# ADR-010: Búsqueda de reservas por email y privacidad

- **Estado:** Aceptado
- **Fecha:** 2026-06

## Contexto

Una reserva se identifica con nombre y email del comprador, sin cuenta de usuario (RF-03, ADR-003). El comprador recibe el identificador (GUID) de su reserva, que actúa como localizador para cancelarla. Pero un comprador que **pierde ese identificador** no tiene forma de recuperar sus reservas, lo que genera fricción real (no recuerda qué reservó, ni puede cancelar).

## Decisión

Se añade `GET /api/reservations/by-email?email=` (público) que devuelve las reservas asociadas a un correo. La comparación es _case-insensitive_. Si el correo no tiene reservas, devuelve una **lista vacía** (no 404), para no revelar por diferencia de respuesta si un correo existe o no en el sistema.

La ruta literal `/by-email` se declara **antes** que la paramétrica `/{id:guid}` para que el enrutador le dé prioridad.

## Justificación

Es un equilibrio entre usabilidad y privacidad. El enunciado modela al comprador como anónimo (sin login), así que exigir autenticación para esta consulta contradiría ese diseño. A la vez, exponer reservas por email tiene una implicación de privacidad evidente: cualquiera que conozca un correo podría listar sus reservas.

Se acepta conscientemente este _tradeoff_ para el alcance de la prueba, documentando la limitación y su mitigación natural:

- **Mitigación actual**: la respuesta no distingue entre "correo sin reservas" y "correo inexistente"; solo expone datos no sensibles de la reserva.
- **Evolución futura (fuera de alcance)**: verificación de propiedad del correo (enlace mágico / OTP por email) antes de revelar las reservas, o mover esta funcionalidad detrás de identidad de usuario.

## Consecuencias

- Mejora la experiencia del comprador sin introducir login.
- Queda registrada la implicación de privacidad y la ruta para endurecerla.
- La cancelación sigue requiriendo el GUID de la reserva como localizador; la búsqueda por email es solo de lectura.
