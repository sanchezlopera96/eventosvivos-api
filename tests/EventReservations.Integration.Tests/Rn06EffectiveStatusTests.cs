using EventReservations.Application.Events.CreateEvent;
using EventReservations.Application.Events.GetEventById;
using EventReservations.Application.Events.ListEvents;
using EventReservations.Application.Events.OccupancyReport;
using EventReservations.Domain.Events;
using EventReservations.Infrastructure.Persistence;
using EventReservations.Infrastructure.Persistence.Queries;
using EventReservations.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Xunit;

namespace EventReservations.Integration.Tests;

/// <summary>
/// RN06: un evento cuya hora de fin ya pasó debe reportarse como "Completado"
/// en las consultas (detalle, ocupación y listado), aunque en la BD siga
/// almacenado como "Activo" (el estado efectivo se calcula en lectura).
/// Estos tests habrían cazado el bug original (RN06 implementada pero nunca
/// invocada).
///
/// Cada evento usa un horario ÚNICO (la BD se comparte entre tests y RN02
/// rechaza eventos solapados en el mismo venue).
/// </summary>
[Collection("postgres")]
public class Rn06EffectiveStatusTests
{
    private readonly PostgresFixture _fx;

    public Rn06EffectiveStatusTests(PostgresFixture fx) => _fx = fx;

    private async Task<T> Exec<T>(Func<AppDbContext, Task<T>> action)
    {
        await using var ctx = _fx.CreateContext();
        return await action(ctx);
    }

    // Horario futuro único por evento (evita solapamientos RN02 en la BD compartida).
    private static DateTime UniqueStart()
    {
        var daysAhead = Random.Shared.Next(200, 5000);
        return DateTime.SpecifyKind(
            DateTime.UtcNow.Date.AddDays(daysAhead).AddHours(18), DateTimeKind.Utc);
    }

    // Crea un evento activo en venue 3 (Arena Sur) con horario único.
    // Devuelve (id, momento de creación, fin del evento).
    private async Task<(Guid Id, DateTime CreatedAt, DateTime EndsAt)> CreateActiveEventAsync()
    {
        var start = UniqueStart();
        // La creación ocurre "antes" del inicio (RF-01 exige inicio futuro).
        var createdAt = start.AddDays(-10);
        var endsAt = start.AddHours(2);

        var id = await Exec(ctx =>
        {
            var handler = new CreateEventCommandHandler(
                new EventRepository(ctx), new VenueRepository(ctx),
                new UnitOfWork(ctx), new TestClock(createdAt));
            return handler.HandleAsync(new CreateEventCommand(
                "Evento que ya terminó", "Evento para verificar el estado efectivo RN06.",
                VenueId: 3, Capacity: 100, StartsAt: start, EndsAt: endsAt,
                Price: 50m, Type: EventType.Concierto));
        });

        return (id, createdAt, endsAt);
    }

    [Fact]
    public async Task OccupancyReport_WhenEventEnded_ReportsCompleted()
    {
        var (eventId, _, endsAt) = await CreateActiveEventAsync();
        var afterEnd = endsAt.AddDays(1);

        // En BD sigue Activo; la consulta usa un reloj posterior al fin.
        var report = await Exec(ctx =>
            new OccupancyReportQueryHandler(ctx, new TestClock(afterEnd))
                .HandleAsync(new OccupancyReportQuery(eventId)));

        report.Status.Should().Be(EventStatus.Completado);
    }

    [Fact]
    public async Task GetEventById_WhenEventEnded_ReportsCompleted()
    {
        var (eventId, _, endsAt) = await CreateActiveEventAsync();
        var afterEnd = endsAt.AddDays(1);

        var detail = await Exec(ctx =>
            new GetEventByIdQueryHandler(ctx, new TestClock(afterEnd))
                .HandleAsync(new GetEventByIdQuery(eventId)));

        detail.Status.Should().Be(EventStatus.Completado);
    }

    [Fact]
    public async Task ListEvents_FilterByCompleted_IncludesEndedEvent()
    {
        var (eventId, _, endsAt) = await CreateActiveEventAsync();
        var afterEnd = endsAt.AddDays(1);

        // Filtra por estado "Completado": el evento terminado debe aparecer,
        // aunque en BD su estado almacenado siga siendo "Activo".
        var list = await Exec(ctx =>
            new ListEventsQueryHandler(ctx, new TestClock(afterEnd))
                .HandleAsync(new ListEventsQuery(
                    Type: null, VenueId: 3, Status: EventStatus.Completado,
                    StartsFrom: null, StartsTo: null, TitleContains: null)));

        list.Should().Contain(e => e.Id == eventId);
    }

    [Fact]
    public async Task ListEvents_FilterByActive_ExcludesEndedEvent()
    {
        var (eventId, _, endsAt) = await CreateActiveEventAsync();
        var afterEnd = endsAt.AddDays(1);

        // El mismo evento NO debe aparecer al filtrar por "Activo" tras su fin.
        var list = await Exec(ctx =>
            new ListEventsQueryHandler(ctx, new TestClock(afterEnd))
                .HandleAsync(new ListEventsQuery(
                    Type: null, VenueId: 3, Status: EventStatus.Activo,
                    StartsFrom: null, StartsTo: null, TitleContains: null)));

        list.Should().NotContain(e => e.Id == eventId);
    }

    [Fact]
    public async Task OccupancyReport_WhenEventNotEnded_ReportsActive()
    {
        var (eventId, createdAt, _) = await CreateActiveEventAsync();

        // Consulta con un reloj posterior a la creación pero anterior al fin:
        // el evento sigue Activo.
        var beforeEnd = createdAt.AddDays(1);
        var report = await Exec(ctx =>
            new OccupancyReportQueryHandler(ctx, new TestClock(beforeEnd))
                .HandleAsync(new OccupancyReportQuery(eventId)));

        report.Status.Should().Be(EventStatus.Activo);
    }
}
