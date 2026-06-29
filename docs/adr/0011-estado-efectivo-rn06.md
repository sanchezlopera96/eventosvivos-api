# ADR-011: Estado efectivo del evento en lectura (RN06)

- **Estado:** Aceptado
- **Fecha:** 2026-06

## Contexto

RN06 exige que un evento se marque como **completado** automáticamente cuando la fecha actual supera su hora de fin. El dominio ya tenía `Event.MarkCompletedIfEnded(now)`, pero ese método **muta** el agregado y nunca se invocaba en el flujo real: no hay un proceso que recorra los eventos y los actualice. En consecuencia, un evento cuya hora de fin ya pasó conservaba `Status = Activo` en la base de datos, y tanto el reporte de ocupación (RF-06) como el filtro por estado (RF-02) reportaban un estado obsoleto.

## Decisión

Se calcula el **estado efectivo en lectura**, sin mutar la base de datos. El agregado expone un método puro:

```csharp
public EventStatus EffectiveStatus(DateTime now)
    => Status == EventStatus.Activo && now > Schedule.EndsAt
        ? EventStatus.Completado
        : Status;
```

Las consultas (`GetEventByIdQueryHandler`, `OccupancyReportQueryHandler`, `ListEventsQueryHandler`) inyectan `TimeProvider` y aplican `EffectiveStatus(now)` al mapear el DTO. En el listado, el filtro por estado se aplica **en memoria sobre el estado efectivo** (no en SQL), para que filtrar por "completado" devuelva los eventos cuyo fin ya pasó aunque en BD sigan "activo".

## Justificación

Recalcular en lectura garantiza que la API **siempre** reporta el estado correcto, sin depender de cuándo se ejecute un proceso de actualización. Un `IHostedService`/job que persista el cambio añadiría complejidad (scheduling, concurrencia) y aun así dejaría ventanas con estado obsoleto entre ejecuciones. El método es puro y sin efectos secundarios, por lo que es seguro usarlo sobre entidades cargadas con `AsNoTracking()`.

## Consecuencias

- La lectura es siempre correcta; el estado almacenado puede quedar "rezagado" hasta que una operación de escritura (o un job futuro) lo reconcilie. Es una diferencia consciente entre estado *almacenado* y estado *efectivo*.
- Cubierto por tests de integración (`Rn06EffectiveStatusTests`): reporte, detalle y listado (filtros activo/completado) sobre un evento cuyo fin ya pasó.
- Evolución futura (fuera de alcance): un `IHostedService` periódico que persista la transición a `Completado` para mantener el estado almacenado alineado, útil si otros procesos consultan la BD directamente.
- `MarkCompletedIfEnded` se conserva para esa eventual reconciliación por escritura.
