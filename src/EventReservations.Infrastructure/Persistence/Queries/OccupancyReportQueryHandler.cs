using EventReservations.Application.Abstractions;
using EventReservations.Application.Common;
using EventReservations.Application.Events.OccupancyReport;
using EventReservations.Domain.Reservations;
using Microsoft.EntityFrameworkCore;

namespace EventReservations.Infrastructure.Persistence.Queries;

public sealed class OccupancyReportQueryHandler
    : IQueryHandler<OccupancyReportQuery, OccupancyReportDto>
{
    private readonly AppDbContext _db;
    private readonly TimeProvider _timeProvider;

    public OccupancyReportQueryHandler(AppDbContext db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    public async Task<OccupancyReportDto> HandleAsync(
        OccupancyReportQuery query, CancellationToken cancellationToken = default)
    {
        var @event = await _db.Events.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == query.EventId, cancellationToken)
            ?? throw NotFoundException.For("evento", query.EventId);

        // Vendidas = solo reservas confirmadas (ADR-004 / RF-06).
        var ticketsSold = await _db.Reservations.AsNoTracking()
            .Where(r => r.EventId == query.EventId && r.Status == ReservationStatus.Confirmada)
            .SumAsync(r => (int?)r.Quantity, cancellationToken) ?? 0;

        // Pendientes = plazas bloqueadas por reservas pendiente_pago. No cuentan
        // como vendidas, pero sí reducen la disponibilidad (ADR-004).
        var pendingSeats = await _db.Reservations.AsNoTracking()
            .Where(r => r.EventId == query.EventId && r.Status == ReservationStatus.PendientePago)
            .SumAsync(r => (int?)r.Quantity, cancellationToken) ?? 0;

        var capacity = @event.Capacity.Value;
        var available = @event.AvailableSeats;              // excluye perdidas (RN07)
        var revenue = ticketsSold * @event.Price.Amount;    // precio × confirmadas
        var occupancy = capacity == 0
            ? 0m
            : Math.Round((decimal)ticketsSold / capacity * 100m, 2);

        // RN06: estado efectivo en lectura (Completado si su fin ya pasó).
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var status = @event.EffectiveStatus(now);

        return new OccupancyReportDto(
            @event.Id, @event.Title, capacity, ticketsSold,
            available, occupancy, revenue, status, pendingSeats);
    }
}
