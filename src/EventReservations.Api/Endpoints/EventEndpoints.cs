using EventReservations.Api.Security;
using EventReservations.Api.Validation;
using EventReservations.Application.Abstractions;
using EventReservations.Application.Events.CancelEvent;
using EventReservations.Application.Events.CreateEvent;
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

        // RF-01: crear evento
        group.MapPost("/", async (
                CreateEventCommand command,
                ICommandHandler<CreateEventCommand, Guid> handler,
                CancellationToken ct) =>
            {
                var id = await handler.HandleAsync(command, ct);
                return Results.Created($"/api/events/{id}", new { id });
            })
            .AddEndpointFilter<AdminApiKeyFilter>()
            .AddEndpointFilter<ValidationFilter<CreateEventCommand>>()
            .WithName("CreateEvent");

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

        // RF-06: reporte de ocupación
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

        // Cancelar evento (cascada a reservas)
        group.MapPost("/{id:guid}/cancel", async (
                Guid id,
                ICommandHandler<CancelEventCommand, int> handler,
                CancellationToken ct) =>
            {
                var cancelledReservations = await handler.HandleAsync(new CancelEventCommand(id), ct);
                return Results.Ok(new { cancelledReservations });
            })
            .AddEndpointFilter<AdminApiKeyFilter>()
            .WithName("CancelEvent");

        return app;
    }
}
