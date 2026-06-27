using EventReservations.Application.Common;
using EventReservations.Application.Events.CreateEvent;
using EventReservations.Domain.Common;
using EventReservations.Domain.Events;
using EventReservations.Domain.Reservations;
using EventReservations.Infrastructure.Persistence;
using EventReservations.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EventReservations.Integration.Tests;

/// <summary>
/// La joya de la corona: demuestra que el sistema NO permite overbooking bajo
/// concurrencia real. Dos reservas compiten por la última plaza; gracias al token
/// xmin (concurrencia optimista, ADR-006), exactamente una tiene éxito.
/// </summary>
[Collection("postgres")]
public class ConcurrencyTests
{
    private readonly PostgresFixture _fx;

    private static readonly DateTime Now = new(2026, 5, 1, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Start = new(2026, 6, 1, 18, 0, 0, DateTimeKind.Utc);

    public ConcurrencyTests(PostgresFixture fx) => _fx = fx;

    [Fact]
    public async Task TwoReservationsForLastSeat_OnlyOneSucceeds()
    {
        // Evento con UNA sola plaza.
        Guid eventId;
        await using (var ctx = _fx.CreateContext())
        {
            var handler = new CreateEventCommandHandler(
                new EventRepository(ctx), new VenueRepository(ctx),
                new UnitOfWork(ctx), new TestClock(Now));
            eventId = await handler.HandleAsync(new CreateEventCommand(
                "Evento Exclusivo", "Un evento con una sola entrada disponible.",
                VenueId: 1, Capacity: 1, StartsAt: Start, EndsAt: Start.AddHours(2),
                Price: 50m, Type: EventType.Conferencia));
        }

        // Barrera para forzar que ambas transacciones lean el evento ANTES de que
        // cualquiera persista: así compiten realmente por la misma plaza.
        using var barrier = new Barrier(2);

        async Task<string> Attempt(string email)
        {
            await using var ctx = _fx.CreateContext();
            var @event = await ctx.Events.FirstAsync(e => e.Id == eventId);

            barrier.SignalAndWait();

            try
            {
                var reservation = @event.Reserve(
                    BuyerInfo.Create("Comprador", email), quantity: 1, Now);
                await ctx.Reservations.AddAsync(reservation);
                await new UnitOfWork(ctx).SaveChangesAsync();
                return "ok";
            }
            catch (ConflictException) { return "conflict"; }   // xmin detectó el choque
            catch (DomainException) { return "no-availability"; }
        }

        var results = await Task.WhenAll(
            Task.Run(() => Attempt("a@correo.com")),
            Task.Run(() => Attempt("b@correo.com")));

        // Exactamente una reserva tuvo éxito; la otra fue rechazada.
        results.Count(r => r == "ok").Should().Be(1);
        results.Count(r => r != "ok").Should().Be(1);

        // Y en la base de datos no hay overbooking: 1 plaza ocupada, 1 reserva.
        await using var verify = _fx.CreateContext();
        var finalEvent = await verify.Events.FirstAsync(e => e.Id == eventId);
        finalEvent.SeatsTaken.Should().Be(1);

        var reservationCount = await verify.Reservations.CountAsync(r => r.EventId == eventId);
        reservationCount.Should().Be(1);
    }
}
