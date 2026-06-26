# ADR-002: CQRS con handlers explícitos (sin MediatR)

- **Estado:** Aceptado
- **Fecha:** 2026-06

## Contexto

Hay una asimetría clara lectura/escritura. Las escrituras (crear evento, reservar,
confirmar pago, cancelar) deben pasar por el dominio y proteger invariantes. Las
lecturas (listado con filtros RF-02, reporte de ocupación RF-06) son proyecciones
que no necesitan cargar agregados completos.

## Decisión

CQRS con interfaces de handler explícitas:

```csharp
public interface ICommandHandler<TCommand, TResult>
public interface IQueryHandler<TQuery, TResult>
```

Los handlers se registran en DI y se inyectan directamente en los endpoints.
**Sin MediatR.**

## Justificación

- Patrón Command/Query explícito y legible, sin despacho por reflexión.
- Para este tamaño, un bus de mediación es maquinaria innecesaria.
- MediatR adoptó licencia comercial en versiones recientes; evitar la dependencia
  elimina una preocupación de licenciamiento en un repo público.
- Las consultas (RF-02, RF-06) proyectan directamente con EF Core, sin pasar por
  el dominio, mejorando el rendimiento.

## Consecuencias

Algo más de cableado de handlers, a cambio de explicitud, cero dependencias de
terceros para el patrón, y separación limpia lectura/escritura.
