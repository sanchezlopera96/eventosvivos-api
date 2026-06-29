# Arquitectura · EventosVivos API

Este documento complementa los [ADRs](adr/) con diagramas. Todos están en Mermaid, que GitHub renderiza de forma nativa.

## 1. Capas (Clean Architecture)

Las dependencias apuntan hacia el dominio. Infrastructure implementa los puertos definidos en Application; Api es el detalle de entrega.

```mermaid
graph TD
    subgraph Api["Api (Minimal APIs)"]
        EP[Endpoints]
        AUTH[Auth / JWT]
        VAL[ValidationFilter]
        ERR[GlobalExceptionHandler]
    end

    subgraph Application["Application (casos de uso · CQRS)"]
        CMD[Command Handlers]
        QRY[Query Handlers]
        PORTS[Puertos: IEventRepository, IReservationRepository,<br/>IVenueRepository, IUnitOfWork]
        DTO[DTOs + Validators]
    end

    subgraph Domain["Domain (núcleo)"]
        AGG[Agregados: Event, Reservation, Venue]
        VO[Value Objects: Capacity, Money,<br/>Schedule, ReservationCode, BuyerInfo]
        RULES[Invariantes de negocio]
    end

    subgraph Infrastructure["Infrastructure"]
        EF[EF Core + AppDbContext]
        REPO[Repositorios]
        QH[Query Handlers de lectura]
        MIG[Migraciones + seed]
    end

    Api --> Application
    Application --> Domain
    Infrastructure --> Domain
    Infrastructure -.implementa.-> PORTS
    EP --> CMD
    EP --> QRY
    CMD --> AGG
    REPO --> EF
```

## 2. Modelo de dominio

```mermaid
classDiagram
    class Event {
        +Guid Id
        +string Title
        +string Description
        +int VenueId
        +Capacity Capacity
        +Schedule Schedule
        +Money Price
        +EventType Type
        +EventStatus Status
        +int SeatsTaken
        +int LostSeats
        +int AvailableSeats
        +Create() Event
        +Update()
        +Reserve() Reservation
        +ReleaseSeats()
        +LoseSeats()
        +Cancel()
        +MarkCompletedIfEnded()
    }

    class Reservation {
        +Guid Id
        +Guid EventId
        +BuyerInfo Buyer
        +int Quantity
        +ReservationStatus Status
        +ReservationCode Code
        +DateTime CreatedAt
        +Confirm()
        +Cancel()
    }

    class Venue {
        +int Id
        +string Name
        +string City
        +int Capacity
    }

    class Capacity {
        +int Value
    }
    class Schedule {
        +DateTime StartsAt
        +DateTime EndsAt
    }
    class Money {
        +decimal Amount
    }
    class BuyerInfo {
        +string Name
        +string Email
    }
    class ReservationCode {
        +string Value
    }

    Event "1" --> "0..*" Reservation : genera
    Event --> Capacity
    Event --> Schedule
    Event --> Money
    Reservation --> BuyerInfo
    Reservation --> ReservationCode
    Event ..> Venue : valida aforo (RN01)
```

## 3. Flujo anti-overbooking (crear y confirmar reserva)

El cupo se bloquea **al crear** la reserva (estado pendiente), no al confirmar. Así dos compradores no pueden llevarse las mismas últimas entradas. La concurrencia se protege con el token `xmin` de PostgreSQL.

```mermaid
sequenceDiagram
    actor Comprador
    participant API as Api
    participant App as CreateReservationHandler
    participant Ev as Event (agregado)
    participant DB as PostgreSQL

    Comprador->>API: POST /api/reservations
    API->>App: CreateReservationCommand
    App->>DB: cargar Event (con xmin)
    App->>Ev: Reserve(buyer, quantity, now)
    Note over Ev: valida estado activo,<br/>cantidad > 0 y<br/>quantity <= AvailableSeats
    Ev-->>App: Reservation (pendiente)<br/>SeatsTaken += quantity
    App->>DB: guardar (chequea xmin)
    alt xmin cambió (otra reserva concurrente)
        DB-->>App: conflicto de concurrencia
        App-->>API: 409 / reintento
    else ok
        DB-->>App: persistido
        App-->>API: 201 Created
    end

    Note over Comprador,DB: Más tarde, el administrador confirma el pago
    Comprador->>API: POST /api/reservations/{id}/confirm (JWT admin)
    API->>App: ConfirmPaymentCommand
    App->>Ev: Confirm() → genera ReservationCode (EV-######)
    App->>DB: guardar
    App-->>API: 200 { code }
```

## 4. Estados

### Evento

```mermaid
stateDiagram-v2
    [*] --> Activo: Create (RF-01, RN01, RN03)
    Activo --> Cancelado: Cancel
    Activo --> Completado: MarkCompletedIfEnded (RN06)
```

### Reserva

```mermaid
stateDiagram-v2
    [*] --> PendientePago: Reserve (bloquea cupo)
    PendientePago --> Confirmada: ConfirmPayment (genera código)
    PendientePago --> Cancelada: Cancel
    Confirmada --> Cancelada: Cancel
    note right of Cancelada
        < 48h: penalización RN07
        (plazas perdidas, no se liberan)
    end note
```
