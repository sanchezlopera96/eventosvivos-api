using EventReservations.Domain.Common;
using EventReservations.Domain.Events;
using EventReservations.Domain.Venues;
using FluentAssertions;
using Xunit;

namespace EventReservations.Domain.Tests.Events;

public class EventTests
{
    // Contexto temporal de referencia. now = domingo 1 de marzo de 2026, 09:00.
    private static readonly DateTime Now = new(2026, 3, 1, 9, 0, 0, DateTimeKind.Utc);

    // Fechas de referencia en 2026 (verificadas):
    //   sábado    = 2026-03-07
    //   domingo   = 2026-03-08
    //   miércoles = 2026-03-11
    private static readonly Venue Auditorio = Venue.Create(1, "Auditorio Central", 200, "Bogotá");

    private static Event CreateValid(
        Schedule? schedule = null,
        int capacity = 100,
        Venue? venue = null,
        string title = "Concierto de Jazz",
        string description = "Una noche de jazz en vivo con artistas invitados.")
    {
        schedule ??= Schedule.Create(
            new DateTime(2026, 3, 11, 18, 0, 0, DateTimeKind.Utc),   // miércoles
            new DateTime(2026, 3, 11, 20, 0, 0, DateTimeKind.Utc));

        return Event.Create(
            title, description, venue ?? Auditorio,
            Capacity.Of(capacity), schedule, Money.Of(50m),
            EventType.Concierto, Now);
    }

    // ---------- Creación: estado inicial ----------

    [Fact]
    public void Create_WhenValid_IsActive()
    {
        var @event = CreateValid();

        @event.Status.Should().Be(EventStatus.Activo);
        @event.VenueId.Should().Be(Auditorio.Id);
        @event.Type.Should().Be(EventType.Concierto);
    }

    // ---------- RF-01: longitudes de texto ----------

    [Theory]
    [InlineData("Jazz")]                       // 4 caracteres (mínimo 5)
    [InlineData("")]
    public void Create_WhenTitleTooShort_Throws(string title)
    {
        var act = () => CreateValid(title: title);

        act.Should().Throw<DomainException>().WithMessage("*título*");
    }

    [Fact]
    public void Create_WhenTitleTooLong_Throws()
    {
        var act = () => CreateValid(title: new string('a', 101));

        act.Should().Throw<DomainException>().WithMessage("*título*");
    }

    [Theory]
    [InlineData("123456789")] // 9 caracteres (mínimo 10)
    public void Create_WhenDescriptionTooShort_Throws(string description)
    {
        var act = () => CreateValid(description: description);

        act.Should().Throw<DomainException>().WithMessage("*descripción*");
    }

    [Fact]
    public void Create_WhenDescriptionTooLong_Throws()
    {
        var act = () => CreateValid(description: new string('a', 501));

        act.Should().Throw<DomainException>().WithMessage("*descripción*");
    }

    // ---------- RN01: capacidad <= capacidad del venue ----------

    [Fact]
    public void Create_WhenCapacityExceedsVenue_Throws()
    {
        // Auditorio Central tiene capacidad 200.
        var act = () => CreateValid(capacity: 201);

        act.Should().Throw<DomainException>().WithMessage("*venue*");
    }

    [Fact]
    public void Create_WhenCapacityEqualsVenue_Succeeds()
    {
        var @event = CreateValid(capacity: 200);

        @event.Capacity.Value.Should().Be(200);
    }

    // ---------- RF-01: fecha de inicio futura ----------

    [Fact]
    public void Create_WhenStartNotFuture_Throws()
    {
        var pastSchedule = Schedule.Create(Now.AddHours(-1), Now.AddHours(1));

        var act = () => CreateValid(schedule: pastSchedule);

        act.Should().Throw<DomainException>().WithMessage("*futura*");
    }

    // ---------- RN03: fin de semana sin inicio después de las 22:00 ----------

    [Fact]
    public void Create_WhenWeekendStartsAfter22_Throws()
    {
        var saturdayLate = Schedule.Create(
            new DateTime(2026, 3, 7, 22, 30, 0, DateTimeKind.Utc),  // sábado 22:30
            new DateTime(2026, 3, 8, 0, 30, 0, DateTimeKind.Utc));

        var act = () => CreateValid(schedule: saturdayLate);

        act.Should().Throw<DomainException>().WithMessage("*22:00*");
    }

    [Fact]
    public void Create_WhenWeekendStartsAt22Exactly_Succeeds()
    {
        var saturdayAt22 = Schedule.Create(
            new DateTime(2026, 3, 7, 22, 0, 0, DateTimeKind.Utc),   // sábado 22:00 exacto
            new DateTime(2026, 3, 7, 23, 30, 0, DateTimeKind.Utc));

        var @event = CreateValid(schedule: saturdayAt22);

        @event.Status.Should().Be(EventStatus.Activo);
    }

    [Fact]
    public void Create_WhenWeekdayStartsAfter22_Succeeds()
    {
        var wednesdayLate = Schedule.Create(
            new DateTime(2026, 3, 11, 23, 0, 0, DateTimeKind.Utc),  // miércoles 23:00
            new DateTime(2026, 3, 12, 1, 0, 0, DateTimeKind.Utc));

        var @event = CreateValid(schedule: wednesdayLate);

        @event.Status.Should().Be(EventStatus.Activo);
    }

    // ---------- Cancelación ----------

    [Fact]
    public void Cancel_WhenActive_SetsCancelled()
    {
        var @event = CreateValid();

        @event.Cancel();

        @event.Status.Should().Be(EventStatus.Cancelado);
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_Throws()
    {
        var @event = CreateValid();
        @event.Cancel();

        var act = () => @event.Cancel();

        act.Should().Throw<DomainException>();
    }

    // ---------- RN06: completado automático ----------

    [Fact]
    public void MarkCompletedIfEnded_WhenNowAfterEnd_SetsCompleted()
    {
        var @event = CreateValid(); // fin = 2026-03-11 20:00

        @event.MarkCompletedIfEnded(new DateTime(2026, 3, 11, 21, 0, 0, DateTimeKind.Utc));

        @event.Status.Should().Be(EventStatus.Completado);
    }

    [Fact]
    public void MarkCompletedIfEnded_WhenNowBeforeEnd_StaysActive()
    {
        var @event = CreateValid();

        @event.MarkCompletedIfEnded(new DateTime(2026, 3, 11, 19, 0, 0, DateTimeKind.Utc));

        @event.Status.Should().Be(EventStatus.Activo);
    }

    [Fact]
    public void Cancel_WhenCompleted_Throws()
    {
        var @event = CreateValid();
        @event.MarkCompletedIfEnded(new DateTime(2026, 3, 11, 21, 0, 0, DateTimeKind.Utc));

        var act = () => @event.Cancel();

        act.Should().Throw<DomainException>();
    }
}
