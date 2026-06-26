# ADR-006: PostgreSQL y concurrencia optimista con `xmin`

- **Estado:** Aceptado
- **Fecha:** 2026-06

## Contexto

El requisito de negocio más crítico es evitar el overbooking con **control en
tiempo real**. Esto implica que dos reservas simultáneas que compitan por las
últimas entradas no puedan tener éxito ambas. La base de datos es de libre
elección.

## Decisión

- **PostgreSQL** como motor de base de datos.
- **Control de concurrencia optimista** usando la columna de sistema `xmin` de
  PostgreSQL como token de concurrencia, mapeada por EF Core con
  `IsRowVersion()` sobre el agregado `Event`.

Cuando dos transacciones leen el mismo evento e intentan reservar a la vez, la
primera en confirmar gana; la segunda falla con `DbUpdateConcurrencyException`,
que la capa de aplicación traduce en un reintento acotado o en una respuesta 409.

## Justificación

- `xmin` es nativo de PostgreSQL: no requiere una columna de versión extra ni
  lógica manual.
- El bloqueo optimista no penaliza el rendimiento en el caso normal (sin
  contención), a diferencia de un bloqueo pesimista.
- Resuelve exactamente el problema central del enunciado de forma demostrable y
  testeable (test de concurrencia con Testcontainers en la fase de integración).

## Alternativas descartadas

- **Bloqueo pesimista (SELECT ... FOR UPDATE)**: correcto, pero serializa el
  acceso y añade contención innecesaria para la carga esperada.
- **Columna de versión manual**: funciona, pero `xmin` lo da el motor sin coste.

## Consecuencias

El agregado `Event` no contiene una propiedad de versión en el dominio; el token
`xmin` se configura como propiedad sombra en la capa de infraestructura,
manteniendo el dominio puro.
