using EventReservations.Api.Validation;
using EventReservations.Application.Abstractions;
using EventReservations.Application.Events.CancelEvent;
using EventReservations.Application.Events.CreateEvent;
using EventReservations.Application.Events.UpdateEvent;
using EventReservations.Application.Events.ListEvents;
using EventReservations.Application.Events.OccupancyReport;
using EventReservations.Application.Events.GetEventById;
using EventReservations.Domain.Events;

namespace EventReservations.Api.Endpoints;

public static class EventEndpoints
{
    public static IEndpointRouteBuilder MapEventEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/events").WithTags("Events");

        // RF-01: crear evento (requiere autenticacion de administrador)
        group.MapPost("/", async (
                CreateEventCommand command,
                ICommandHandler<CreateEventCommand, Guid> handler,
                CancellationToken ct) =>
            {
                var id = await handler.HandleAsync(command, ct);
                return Results.Created($"/api/events/{id}", new { id });
            })
            .RequireAuthorization("Admin")
            .AddEndpointFilter<ValidationFilter<CreateEventCommand>>()
            .WithName("CreateEvent");

        // RF (editar): actualizar un evento activo (requiere administrador)
        group.MapPut("/{id:guid}", async (
                Guid id,
                UpdateEventCommand body,
                ICommandHandler<UpdateEventCommand, Guid> handler,
                CancellationToken ct) =>
            {
                // El id viaja en la ruta; se sobreescribe el del body por seguridad.
                var command = body with { EventId = id };
                var updatedId = await handler.HandleAsync(command, ct);
                return Results.Ok(new { id = updatedId });
            })
            .RequireAuthorization("Admin")
            .AddEndpointFilter<ValidationFilter<UpdateEventCommand>>()
            .WithName("UpdateEvent");

        // RF-02: listar eventos con filtros opcionales (query string)
        group.MapGet("/", async (
                IQueryHandler<ListEventsQuery, IReadOnlyList<EventListItemDto>> handler,
                CancellationToken ct,
                EventType? type,
                int? venueId,
                EventStatus? status,
                DateTime? startsFrom,
                DateTime? startsTo,
                string? title) =>
            {
                var query = new ListEventsQuery(type, venueId, status, startsFrom, startsTo, title);
                var events = await handler.HandleAsync(query, ct);
                return Results.Ok(events);
            })
            .WithName("ListEvents");

        // RF-06: reporte de ocupacion
        group.MapGet("/{id:guid}/occupancy", async (
                Guid id,
                IQueryHandler<OccupancyReportQuery, OccupancyReportDto> handler,
                CancellationToken ct) =>
            {
                var report = await handler.HandleAsync(new OccupancyReportQuery(id), ct);
                return Results.Ok(report);
            })
            .WithName("OccupancyReport");

        // Obtener un evento por id
        group.MapGet("/{id:guid}", async (
                Guid id,
                IQueryHandler<GetEventByIdQuery, EventDetailDto> handler,
                CancellationToken ct) =>
        {
            var ev = await handler.HandleAsync(new GetEventByIdQuery(id), ct);
            return Results.Ok(ev);
        })
            .WithName("GetEventById");

        // Cancelar evento (cascada a reservas) - requiere administrador
        group.MapPost("/{id:guid}/cancel", async (
                Guid id,
                ICommandHandler<CancelEventCommand, int> handler,
                CancellationToken ct) =>
            {
                var cancelledReservations = await handler.HandleAsync(new CancelEventCommand(id), ct);
                return Results.Ok(new { cancelledReservations });
            })
            .RequireAuthorization("Admin")
            .WithName("CancelEvent");

        return app;
    }
}
