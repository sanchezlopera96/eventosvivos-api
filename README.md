# EventosVivos — API de Gestión de Eventos y Reservas

API REST para el núcleo del sistema de reservas de **EventosVivos**: creación de
eventos, control de aforo en tiempo real, reservas con ciclo de pago y reportes
de ocupación.

> **Estado:** en desarrollo. Este README se completará al final con instrucciones
> de ejecución, arquitectura y despliegue.

## Stack

- .NET 10 / ASP.NET Core
- Clean Architecture + DDD táctico
- CQRS (handlers explícitos, sin MediatR)
- PostgreSQL + EF Core
- FluentValidation
- xUnit + FluentAssertions + Moq

## Estructura

```
src/
  EventReservations.Domain          Núcleo de negocio (entidades, agregados, VOs, reglas)
  EventReservations.Application     Casos de uso (CQRS), DTOs, validaciones, interfaces
  EventReservations.Infrastructure  EF Core, PostgreSQL, repositorios
  EventReservations.Api             Endpoints REST, DI, middleware
tests/
  EventReservations.Domain.Tests        Pruebas de reglas de negocio (TDD)
  EventReservations.Application.Tests   Pruebas de casos de uso
docs/
  adr/                              Architecture Decision Records
```

## Documentación de decisiones

Las decisiones de arquitectura del sistema (incluido el frontend) se documentan
como ADRs en [`docs/adr`](docs/adr). El repositorio del frontend Angular es
**[eventosvivos-web](https://github.com/TU_USUARIO/eventosvivos-web)** y enlaza a
estos ADRs.

## Cómo ejecutar los tests

```bash
dotnet test
```
