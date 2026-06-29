# EventosVivos · API

API REST para la gestión de eventos culturales y reserva de entradas, con **control de aforo en tiempo real** para eliminar el _overbooking_. Es el backend de la prueba técnica EventosVivos (.NET + Angular).

## Enlaces de producción

| Recurso | URL |
|---------|-----|
| **API (base)** | https://eventosvivos-api-ceiba-bse2accpaub5htgs.centralus-01.azurewebsites.net |
| **Swagger / OpenAPI** | https://eventosvivos-api-ceiba-bse2accpaub5htgs.centralus-01.azurewebsites.net/swagger/index.html |
| **Health check** | https://eventosvivos-api-ceiba-bse2accpaub5htgs.centralus-01.azurewebsites.net/health |
| **Frontend (SPA Angular)** | https://nice-tree-0da071c10.7.azurestaticapps.net |

---

## Tabla de contenido

- [Descripción](#descripción)
- [Tecnologías](#tecnologías)
- [Arquitectura](#arquitectura)
- [Estructura de carpetas](#estructura-de-carpetas)
- [Reglas de negocio](#reglas-de-negocio)
- [Requisitos](#requisitos)
- [Instalación y ejecución local](#instalación-y-ejecución-local)
- [Docker](#docker)
- [Variables de entorno](#variables-de-entorno)
- [Autenticación](#autenticación)
- [Documentación de la API (endpoints)](#documentación-de-la-api-endpoints)
- [Guía de pruebas en Swagger](#guía-de-pruebas-en-swagger)
- [Pruebas automatizadas](#pruebas-automatizadas)
- [Convenciones y buenas prácticas](#convenciones-y-buenas-prácticas)
- [CI/CD](#cicd)
- [Decisiones de arquitectura (ADR)](#decisiones-de-arquitectura-adr)
- [Autor](#autor)
- [Licencia](#licencia)

---

## Descripción

EventosVivos es una _startup_ cultural que vende entradas para eventos en vivo. Su dolor principal es el **overbooking**: vender más entradas que el aforo por no controlar la disponibilidad en tiempo real. Esta API resuelve ese problema modelando el evento como **frontera de consistencia del aforo**: cada reserva bloquea cupo en el momento de crearse, no al pagar, de modo que dos compradores no puedan llevarse las mismas últimas entradas.

El servicio expone una API REST con dos superficies: una **pública** (catálogo de eventos, creación y cancelación de reservas, consulta por correo, reporte de ocupación) y una de **administración** protegida con JWT (alta/edición/cancelación de eventos y confirmación de pagos). Incluye reglas de negocio de calendario y penalización, reporte de ocupación e ingresos, y control de concurrencia a nivel de base de datos.

## Tecnologías

- **.NET 10** · Minimal APIs
- **PostgreSQL 16** · Entity Framework Core 10 (Npgsql)
- **FluentValidation** — validación de entrada
- **JWT (Bearer)** + **BCrypt** — autenticación de administrador
- **Swagger / OpenAPI** — documentación interactiva
- **xUnit** + **Moq** + **FluentAssertions** — pruebas unitarias e integración
- **Docker / Docker Compose** — PostgreSQL en local
- **GitHub Actions** — CI/CD
- **Azure App Service** (Central US) — hosting de la API

## Arquitectura

**Clean Architecture** con dominio rico (DDD) y separación **CQRS** de comandos y consultas. Las dependencias apuntan siempre hacia el centro; la capa de dominio no conoce a ninguna otra.

```
Api  →  Application  →  Domain  ←  Infrastructure
                                   (implementa los puertos de Application)
```

- **Domain**: agregados (`Event`, `Reservation`, `Venue`), _value objects_ (`Capacity`, `Money`, `Schedule`, `ReservationCode`, `BuyerInfo`), enums de estado e invariantes de negocio. Sin dependencias de framework.
- **Application**: casos de uso como _command/query handlers_ (CQRS sin MediatR), puertos (`IEventRepository`, `IReservationRepository`, `IVenueRepository`, `IUnitOfWork`), validadores y DTOs.
- **Infrastructure**: EF Core, configuraciones de mapeo, repositorios, _query handlers_ de lectura, `UnitOfWork`, migraciones y _seed_ de venues.
- **Api**: Minimal APIs, autenticación JWT, filtros de validación, manejo global de errores (`ProblemDetails`), Swagger, CORS y rate limiting.

Diagramas detallados (capas, modelo de dominio, flujo anti-overbooking y máquinas de estado) en [`docs/architecture.md`](docs/architecture.md).

## Estructura de carpetas

```text
src/
├── EventReservations.Domain/          # Núcleo: agregados, VOs, reglas de negocio
│   ├── Common/                        # Entity, Money, DomainException
│   ├── Events/                        # Event, Capacity, Schedule, EventType/Status
│   ├── Reservations/                  # Reservation, BuyerInfo, ReservationCode...
│   └── Venues/                        # Venue
├── EventReservations.Application/     # Casos de uso (CQRS)
│   ├── Abstractions/                  # Puertos (repos, UoW, handlers)
│   ├── Events/                        # CreateEvent, UpdateEvent, ListEvents...
│   └── Reservations/                  # CreateReservation, ConfirmPayment...
├── EventReservations.Infrastructure/  # EF Core, repos, queries, migraciones
│   └── Persistence/
└── EventReservations.Api/             # Minimal APIs, auth, validación, errores
    ├── Auth/  Endpoints/  Errors/  Validation/

tests/
├── EventReservations.Domain.Tests/        # Reglas de negocio (TDD)
├── EventReservations.Application.Tests/    # Casos de uso (con dobles)
└── EventReservations.Integration.Tests/    # API end-to-end (WebApplicationFactory)

docs/
├── adr/                               # Architecture Decision Records
└── architecture.md                    # Diagramas (Mermaid)
```

## Reglas de negocio

El agregado `Event` es la frontera de consistencia del aforo.

| Regla | Descripción | Dónde se aplica |
|-------|-------------|-----------------|
| RN01 | La capacidad del evento no puede exceder el aforo del venue | Dominio (`Event.Create/Update`) |
| RN02 | No puede haber dos eventos activos solapados en el mismo venue | Aplicación |
| RN03 | En fin de semana no se permite iniciar después de las 22:00 | Dominio |
| RN04 | No se puede reservar a menos de 1h del inicio | Aplicación (requiere reloj) |
| RN05 | Restricción por precio (> \$100) | Aplicación |
| RN06 | Un evento pasada su fecha de fin se marca "completado" | Dominio |
| RN07 | Cancelar con < 48h penaliza: las plazas se "pierden" (no vuelven a la venta) | Dominio |
| RF-01 | La fecha de inicio debe ser futura | Dominio |
| RF-03 | Una reserva se crea con nombre + email (sin cuenta), en estado pendiente | Dominio/Aplicación |
| RF-06 | Reporte de ocupación (ver abajo) | Aplicación |

### Modelo de aforo (clave)

Una reserva en **`pendiente_pago` ya bloquea cupo** (ver [ADR-004](docs/adr/0004-modelo-de-aforo.md)):

```
SeatsTaken      = plazas de reservas pendientes + confirmadas
LostSeats       = plazas perdidas por penalización RN07
AvailableSeats  = Capacity − SeatsTaken − LostSeats
```

### Reporte de ocupación (RF-06)

Por cada evento: **entradas vendidas** (solo confirmadas), **disponibles restantes** (`AvailableSeats`, excluye las perdidas por RN07), **porcentaje de ocupación**, **ingresos** (precio × confirmadas) y **estado** (activo / cancelado / completado).

## Requisitos

- **.NET SDK 10**
- **Docker Desktop** (para PostgreSQL en local)
- **dotnet-ef** (`dotnet tool install --global dotnet-ef`)

## Instalación y ejecución local

### Clonar el repositorio

```bash
git clone https://github.com/sanchezlopera96/eventosvivos-api.git
cd eventosvivos-api
```

### Restaurar dependencias

```bash
dotnet restore
```

### Levantar PostgreSQL

```bash
docker compose up -d
```

### Configurar secretos de desarrollo

Crear `src/EventReservations.Api/appsettings.Development.json` (ignorado por git) con la cadena de conexión, la sección `Jwt` y las credenciales de administrador. Ver [Variables de entorno](#variables-de-entorno).

### Ejecutar migraciones

```bash
dotnet ef database update \
  --project src/EventReservations.Infrastructure \
  --startup-project src/EventReservations.Api
```

Esto crea el esquema y siembra los venues: Auditorio Central (Bogotá, aforo 200), Sala Norte (Bogotá, 50), Arena Sur (Medellín, 500).

### Ejecutar la aplicación

```bash
dotnet run --project src/EventReservations.Api
```

La API queda en `https://localhost:7xxx`, con Swagger en `/swagger`.

## Docker

La base de datos se levanta con Docker Compose:

```bash
docker compose up -d        # inicia PostgreSQL 16
docker compose down         # detiene y elimina el contenedor
```

> La API se ejecuta con `dotnet run` en desarrollo. El despliegue en producción se realiza en Azure App Service mediante CI/CD (ver [CI/CD](#cicd)).

## Variables de entorno

En Azure las claves anidadas usan doble guion bajo (`__`). En local se definen en `appsettings.Development.json`.

| Variable | Descripción |
|----------|-------------|
| `ConnectionStrings__Default` | Cadena de conexión a PostgreSQL |
| `Jwt__Issuer` | Emisor del token |
| `Jwt__Audience` | Audiencia del token |
| `Jwt__SigningKey` | Clave de firma (secreto) |
| `AdminCredentials__Username` | Usuario administrador |
| `AdminCredentials__PasswordHash` | Hash BCrypt de la contraseña |
| `Cors__AllowedOrigins__0..n` | Orígenes permitidos (p. ej. el dominio del SPA) |
| `Swagger__Enabled` | Habilita Swagger fuera de desarrollo |

Los secretos nunca se versionan: `appsettings.Development.json` está en `.gitignore` y en Azure se configuran como variables de entorno del App Service.

## Autenticación

La superficie pública no requiere credenciales; la cancelación de reservas usa el identificador (GUID no adivinable) como localizador. La superficie de **administración** requiere un **JWT** (ver [ADR-008](docs/adr/0008-jwt-admin.md)).

```http
POST /api/auth/login
Content-Type: application/json

{ "username": "<usuario>", "password": "<contraseña>" }
```

Respuesta: `{ "token": "...", "expiresAt": "..." }`. El token se envía como `Authorization: Bearer <token>`. Si no hay credenciales configuradas, el login responde **503** ("seguro por defecto").

## Documentación de la API (endpoints)

### Públicos

| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/api/events` | Lista eventos con filtros opcionales (`type`, `venueId`, `status`, `startsFrom`, `startsTo`, `title`) |
| GET | `/api/events/{id}` | Detalle de un evento |
| GET | `/api/events/{id}/occupancy` | Reporte de ocupación del evento (RF-06) |
| POST | `/api/reservations` | Crea una reserva (pendiente de pago) |
| POST | `/api/reservations/{id}/cancel` | Cancela una reserva (localizador = id) |
| GET | `/api/reservations/by-email?email=` | Reservas asociadas a un correo |
| GET | `/api/reservations/{id}` | Detalle de una reserva |
| POST | `/api/auth/login` | Obtiene un JWT de administrador |

### Administración (requieren JWT)

| Método | Ruta | Descripción |
|--------|------|-------------|
| POST | `/api/events` | Crea un evento |
| PUT | `/api/events/{id}` | Edita un evento activo |
| POST | `/api/events/{id}/cancel` | Cancela un evento |
| GET | `/api/reservations` | Lista reservas (filtro opcional por estado) |
| POST | `/api/reservations/{id}/confirm` | Confirma el pago (devuelve el código EV-######) |

Los errores siguen `ProblemDetails`: violaciones de regla de negocio → **422**; recursos inexistentes → **404**; validación de entrada → **400**; no autorizado → **401**.

### Ejemplo: crear una reserva

```http
POST /api/reservations
Content-Type: application/json

{
  "eventId": "00000000-0000-0000-0000-000000000000",
  "quantity": 2,
  "buyerName": "Ana Pérez",
  "buyerEmail": "ana@example.com"
}
```

## Guía de pruebas en Swagger

Flujo recomendado para probar la API extremo a extremo desde `/swagger`:

1. **Listar eventos** — `GET /api/events`. Copia el `id` de un evento activo.
2. **Crear una reserva** — `POST /api/reservations` con ese `eventId`, `quantity`, `buyerName` y `buyerEmail`. La reserva queda pendiente y **bloquea cupo**. Copia el `id` devuelto.
3. **Ver ocupación** — `GET /api/events/{id}/occupancy`: `availableSeats` baja, pero "vendidas" sigue en 0 (aún no se confirma el pago).
4. **Autenticarse como admin** — `POST /api/auth/login`. Copia el `token` y pulsa **Authorize** (candado) en Swagger, introduciendo `Bearer <token>`.
5. **Confirmar el pago** — `POST /api/reservations/{id}/confirm`. Devuelve el código `EV-######`; la reserva cuenta como vendida.
6. **Reporte tras la venta** — `GET /api/events/{id}/occupancy`: "vendidas" e "ingresos" reflejan la confirmación.
7. **Buscar por correo** — `GET /api/reservations/by-email?email=ana@example.com`.
8. **Casos de regla** — intenta reservar más entradas que `availableSeats` (→ 422) o crear un evento sin token (→ 401).

## Pruebas automatizadas

```bash
dotnet test
```

Con cobertura:

```bash
dotnet test --collect:"XPlat Code Coverage"
```

Tres niveles:

- **Domain.Tests** — reglas del agregado (creación, aforo, RN03, RN07…), con enfoque TDD.
- **Application.Tests** — casos de uso con dobles de prueba (Moq) sobre los puertos.
- **Integration.Tests** — API extremo a extremo con `WebApplicationFactory`: auth (401), casos felices (200/201) y reglas (422/404).

> En Windows con Smart App Control activo, las DLL de test recién compiladas pueden quedar bloqueadas y omitir la suite de integración en local; la CI en GitHub Actions (Linux) ejecuta la suite completa.

## Convenciones y buenas prácticas

- **Clean Architecture** + **DDD** (dominio rico, value objects, invariantes en el agregado).
- **CQRS** sin MediatR (handlers explícitos de comando/consulta).
- **SOLID** y dependencias hacia el dominio (inversión de dependencias vía puertos).
- **Conventional Commits** y desarrollo incremental por _feature branches_ + Pull Request.
- **TDD** para las reglas de negocio.
- **ProblemDetails** para errores; sin filtrar detalles internos en 5xx.
- **Seguro por defecto**: superficie administrativa inaccesible si no hay credenciales; secretos fuera del repositorio.

## CI/CD

**GitHub Actions** automatiza build, pruebas y despliegue:

- En cada push a `main`, el workflow compila la solución, ejecuta los tests (Linux) y, si pasan, despliega a **Azure App Service** (Central US).
- Las migraciones de base de datos se aplican en el arranque de la aplicación.
- Los secretos se inyectan como variables de entorno del App Service; nunca se versionan.

## Decisiones de arquitectura (ADR)

Registradas en [`docs/adr/`](docs/adr/):

| ADR | Decisión |
|-----|----------|
| 0000 | Plan de trabajo y proceso de desarrollo |
| 0001 | Clean Architecture + DDD |
| 0002 | CQRS sin MediatR |
| 0003 | _(Supersedida por 0008)_ Autenticación fuera de alcance |
| 0004 | Modelo de aforo: `pendiente_pago` bloquea disponibilidad |
| 0005 | Dos repositorios + Unit of Work |
| 0006 | PostgreSQL: concurrencia optimista con `xmin` |
| 0007 | Estrategia de seguridad de la API |
| 0008 | Autenticación de administrador con JWT |
| 0009 | Edición de eventos |
| 0010 | Búsqueda de reservas por email y privacidad |

## Autor

**sanchezlopera96** — [GitHub](https://github.com/sanchezlopera96)

## Licencia

Proyecto desarrollado como prueba técnica. Licencia no especificada.
