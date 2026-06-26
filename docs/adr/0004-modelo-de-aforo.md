# ADR-004: Modelo de aforo — `pendiente_pago` bloquea disponibilidad

- **Estado:** Aceptado
- **Fecha:** 2026-06

## Contexto

El dolor de negocio central de EventosVivos es el **overbooking**: venden más
entradas que la capacidad porque no tienen control en tiempo real. El enunciado
define tres estados de reserva (`pendiente_pago`, `confirmada`, `cancelada`) y un
reporte (RF-06) donde "vendidas = confirmadas".

¿Una reserva en `pendiente_pago` ocupa cupo, o solo lo ocupan las confirmadas?

## Decisión

Una reserva en `pendiente_pago` **sí bloquea disponibilidad**. La disponibilidad
se calcula descontando reservas `pendiente_pago` + `confirmada`.

El reporte RF-06 distingue dos conceptos:
- **Vendidas / ingresos**: solo `confirmada` (precio × entradas confirmadas).
- **Disponibilidad restante**: capacidad − (pendientes + confirmadas), excluyendo
  además las entradas "perdidas" por penalización (RN07).

## Justificación

Si `pendiente_pago` no bloqueara cupo, dos compradores podrían reservar las
últimas entradas a la vez y ambos llegar a pago, reproduciendo el overbooking que
el sistema debe eliminar. Bloquear al crear la reserva es la única forma de
garantizar control en tiempo real.

## Consecuencias

- El agregado `Event` descuenta cupo al **crear** la reserva, no al confirmar.
- Cancelar una reserva libera cupo, salvo penalización RN07 (entradas "perdidas":
  no se liberan para venta pero siguen contando en el reporte).
- La concurrencia al crear reservas se protege con el token `xmin` (ADR-006).
- Mejora futura fuera de alcance: expirar reservas `pendiente_pago` tras un tiempo
  para devolver cupo automáticamente. Se documenta como no implementado.
