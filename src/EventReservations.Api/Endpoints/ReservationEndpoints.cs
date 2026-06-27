using EventReservations.Api.Validation;
using EventReservations.Application.Abstractions;
using EventReservations.Application.Reservations.CancelReservation;
using EventReservations.Application.Reservations.ConfirmPayment;
using EventReservations.Application.Reservations.CreateReservation;
using EventReservations.Domain.Reservations;

namespace EventReservations.Api.Endpoints;

public static class ReservationEndpoints
{
    public static IEndpointRouteBuilder MapReservationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reservations").WithTags("Reservations");

        // RF-03: reservar entradas
        group.MapPost("/", async (
                CreateReservationCommand command,
                ICommandHandler<CreateReservationCommand, Guid> handler,
                CancellationToken ct) =>
            {
                var id = await handler.HandleAsync(command, ct);
                return Results.Created($"/api/reservations/{id}", new { id });
            })
            .AddEndpointFilter<ValidationFilter<CreateReservationCommand>>()
            .WithName("CreateReservation");

        // RF-04: confirmar pago (devuelve el código EV-######)
        group.MapPost("/{id:guid}/confirm", async (
                Guid id,
                ICommandHandler<ConfirmPaymentCommand, string> handler,
                CancellationToken ct) =>
            {
                var code = await handler.HandleAsync(new ConfirmPaymentCommand(id), ct);
                return Results.Ok(new { code });
            })
            .WithName("ConfirmPayment");

        // RF-05: cancelar reserva (devuelve el efecto sobre el aforo)
        group.MapPost("/{id:guid}/cancel", async (
                Guid id,
                ICommandHandler<CancelReservationCommand, CancellationOutcome> handler,
                CancellationToken ct) =>
            {
                var outcome = await handler.HandleAsync(new CancelReservationCommand(id), ct);
                return Results.Ok(new { outcome = outcome.ToString() });
            })
            .WithName("CancelReservation");

        return app;
    }
}
