# Convención de trabajo con Git

Flujo ligero pero deliberado, para que el historial refleje el proceso de
desarrollo y las decisiones tomadas.

## Ramas

- `main`: siempre estable y desplegable.
- `feature/<nombre>`: una rama por incremento lógico, integrada a `main` por PR.

Ejemplos: `feature/domain-model`, `feature/application-cqrs`,
`feature/infrastructure`, `feature/rest-api`.

## Commits (Conventional Commits)

Formato: `tipo(ámbito): descripción`

- `feat`: nueva funcionalidad o regla de negocio.
- `fix`: corrección.
- `refactor`: cambio interno sin alterar comportamiento.
- `test`: añadir o ajustar pruebas.
- `docs`: documentación (incluye ADRs).
- `chore`: mantenimiento (estructura, CI, gitignore).

## Pull Requests

Cada feature se integra por PR, aunque el desarrollo sea individual. La
descripción explica el *qué* y el *porqué*, enlaza el ADR relevante y resume los
tests añadidos. Deja el razonamiento documentado y revisable.

## TDD

Para reglas de negocio: primero el test (rojo), luego la implementación (verde),
luego refactor. El historial muestra esa secuencia.
