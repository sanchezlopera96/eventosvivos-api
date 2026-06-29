using System.Text.RegularExpressions;
using EventReservations.Application.Events.CreateEvent;
using EventReservations.Application.Events.OccupancyReport;
using EventReservations.Application.Reservations.ConfirmPayment;
using EventReservations.Application.Reservations.CreateReservation;
using EventReservations.Domain.Events;
using EventReservations.Infrastructure.Persistence;
using EventReservations.Infrastructure.Persistence.Queries;
using EventReservations.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Xunit;

namespace EventReservations.Integration.Tests;

[Collection("postgres")]
public class ReservationFlowTests
{
    private readonly PostgresFixture _fx;

    // now lejano al evento: sin límites por transacción (RN04/RN05/RF-03).
    private static readonly DateTime Now = new(2026, 5, 1, 9, 0, 0, DateTimeKind.Utc);
    // Evento el lunes 2026-06-01 (día de semana).
    private static readonly DateTime Start = new(2026, 6, 1, 18, 0, 0, DateTimeKind.Utc);

    public ReservationFlowTests(PostgresFixture fx) => _fx = fx;

    private async Task<T> Exec<T>(Func<AppDbContext, Task<T>> action)
    {
        await using var ctx = _fx.CreateContext();
        return await action(ctx);
    }

    [Fact]
    public async Task FullFlow_Create_Reserve_Confirm_Report()
    {
        // 1) Crear evento. Usamos el venue 2 (Sala Norte) para aislar este test
        //    de ConcurrencyTests, que usa el venue 1 (ambos comparten el contenedor).
        var eventId = await Exec(ctx =>
        {
            var handler = new CreateEventCommandHandler(
                new EventRepository(ctx), new VenueRepository(ctx),
                new UnitOfWork(ctx), new TestClock(Now));
            return handler.HandleAsync(new CreateEventCommand(
                "Concierto de Jazz", "Una noche de jazz en vivo con artistas invitados.",
                VenueId: 2, Capacity: 50, StartsAt: Start, EndsAt: Start.AddHours(2),
                Price: 50m, Type: EventType.Concierto));
        });

        // 2) Reservar 2 entradas.
        var reservationId = await Exec(ctx =>
        {
            var handler = new CreateReservationCommandHandler(
                new EventRepository(ctx), new ReservationRepository(ctx),
                new UnitOfWork(ctx), new TestClock(Now));
            return handler.HandleAsync(new CreateReservationCommand(
                eventId, Quantity: 2, BuyerName: "Ana Pérez", BuyerEmail: "ana@correo.com"));
        });

        // 3) Confirmar pago -> genera código EV-######.
        var code = await Exec(ctx =>
        {
            var handler = new ConfirmPaymentCommandHandler(
                new ReservationRepository(ctx), new UnitOfWork(ctx), new TestClock(Now));
            return handler.HandleAsync(new ConfirmPaymentCommand(reservationId));
        });

        Regex.IsMatch(code, @"^EV-\d{6}$").Should().BeTrue();

        // 4) Reporte de ocupación.
        var report = await Exec(ctx =>
            new OccupancyReportQueryHandler(ctx, new TestClock(Now))
                .HandleAsync(new OccupancyReportQuery(eventId)));

        report.TicketsSold.Should().Be(2);
        report.TotalRevenue.Should().Be(100m);   // 50 x 2 confirmadas
        report.AvailableSeats.Should().Be(48);   // 50 - 2 reservadas
        report.Status.Should().Be(EventStatus.Activo);
    }
}
