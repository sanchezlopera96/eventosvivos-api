# ADR-009: Edición de eventos

- **Estado:** Aceptado
- **Fecha:** 2026-06

## Contexto

El sistema permite crear y cancelar eventos. Para la operación real, un administrador necesita **corregir o ajustar** un evento ya publicado (título, descripción, sede, fecha, precio, capacidad) sin tener que cancelarlo y recrearlo, lo que rompería las reservas existentes.

## Decisión

Se añade `PUT /api/events/{id}` (solo administrador) que actualiza un evento mediante el método de dominio `Event.Update(...)`. La edición aplica las **mismas invariantes que la creación** más una regla específica de la edición:

- Solo se pueden editar eventos en estado **Activo**.
- RN01: la capacidad no puede exceder el aforo del venue.
- RN03 y RF-01: curfew de fin de semana y fecha de inicio futura.
- **Nueva regla**: la capacidad no puede reducirse por debajo de las plazas ya ocupadas (`SeatsTaken + LostSeats`), para no invalidar reservas existentes.
- RN02 (no solapamiento de eventos activos en el mismo venue) se valida en la capa de aplicación, que dispone del contexto de los demás eventos, **excluyendo el propio evento** de la comprobación.

La validación de forma (longitudes, obligatorios) la hace FluentValidation antes del handler; las invariantes las garantiza el agregado.

## Justificación

Centralizar la edición en `Event.Update` reutiliza las mismas reglas que `Create`, evitando duplicación y divergencias. Mantener la regla "capacidad ≥ ocupadas" en el dominio protege la consistencia del aforo: sin ella, se podría dejar un evento con más entradas vendidas que su capacidad, reintroduciendo el overbooking que el sistema combate.

## Consecuencias

- El endpoint exige autorización de administrador (ADR-008).
- En el frontend, la edición reutiliza el formulario de creación precargado; el botón de editar se deshabilita para eventos no activos, reflejando la regla del dominio.
- Las reglas RN04/RN05 (límites por transacción de reserva) no aplican a la edición de eventos.
