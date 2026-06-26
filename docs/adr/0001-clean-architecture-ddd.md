# ADR-001: Clean Architecture con DDD táctico

- **Estado:** Aceptado
- **Fecha:** 2026-06

## Contexto

El sistema tiene un núcleo de reglas de negocio rico: control de aforo en tiempo
real (el dolor central de EventosVivos es el overbooking), máquina de estados de
reservas, restricciones temporales (RN03, RN04, RF-03), superposición de venues
(RN02) y reportes (RF-06). El enunciado deja la arquitectura a libre elección y
evalúa la calidad de la decisión de diseño.

## Decisión

Clean Architecture en cuatro capas con DDD táctico:

```
Api → Application → Domain ← Infrastructure
```

- **Domain**: agregados (`Event`, `Reservation`), value objects, reglas de
  negocio. Sin dependencias externas.
- **Application**: casos de uso (CQRS), DTOs, validaciones, interfaces de
  repositorio.
- **Infrastructure**: EF Core, PostgreSQL, repositorios.
- **Api**: endpoints REST, DI, middleware, manejo de errores.

La regla de dependencias apunta siempre hacia el dominio.

## Justificación

Las reglas de negocio son el corazón evaluado de la prueba. Aislarlas en un
dominio puro las hace testeables sin infraestructura (TDD trivial) y concentra
las invariantes en un solo lugar, evitando que se filtren a la persistencia o a
los controladores.

## Alternativas descartadas

- **N-capas tradicional**: acopla negocio y persistencia, dificulta el testeo.
- **Vertical Slice / microservicios**: sobreingeniería para el alcance.

## Consecuencias

Más proyectos y algo de ceremonia inicial, a cambio de un núcleo de negocio
limpio, testeable y con límites claros.
