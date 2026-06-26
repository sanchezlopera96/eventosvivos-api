# ADR-000: Plan de trabajo y proceso de desarrollo

- **Estado:** Aceptado
- **Fecha:** 2026-06

## Contexto

Prueba técnica para EventosVivos (.NET + Angular), plazo 3 días. El enunciado
entrega como obligatorio un repositorio público y evalúa explícitamente *la
calidad de la decisión, no la decisión en sí*. Por tanto, el historial de Git y
la documentación de decisiones forman parte de la entrega.

## Decisión

Desarrollo incremental: cada bloque de trabajo va en una rama de feature
integrada por Pull Request, con commits semánticos (Conventional Commits) y TDD
para las reglas de negocio. Las decisiones de arquitectura se registran como ADRs.

### Secuencia de incrementos

```
main
 ├─ chore: estructura base de la solucion
 ├─ docs: ADRs de arquitectura y convencion de trabajo
 ├─ feature/domain-model        PR #1  dominio + reglas RN01, RN03, RN06 (TDD)
 ├─ feature/application-cqrs     PR #2  casos de uso + RN02, RN04, RN05, RF-03
 ├─ feature/infrastructure       PR #3  EF Core, PostgreSQL, seed venues, xmin
 ├─ feature/rest-api             PR #4  endpoints REST, ProblemDetails, Swagger
 ├─ feature/devops               PR #5  Docker, GitHub Actions, Azure
 └─ feature/docs                 PR #6  README final, diagramas C4
```

El frontend Angular vive en un repositorio separado (ver ADR-005).

## Nota sobre el proceso

El modelo de dominio inicial se esbozó partiendo de supuestos razonables. Al
recibir el enunciado detallado se detectaron divergencias con los requisitos
reales (estados de la reserva, concepto de entradas "perdidas", reglas de venue).
El plan parte de cero alineado al enunciado, y este registro deja constancia de
que el desarrollo fue deliberado, no un volcado final de código.
