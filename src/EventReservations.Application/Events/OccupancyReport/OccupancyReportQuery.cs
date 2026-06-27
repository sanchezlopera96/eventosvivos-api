using EventReservations.Domain.Events;

namespace EventReservations.Application.Events.OccupancyReport;

/// <summary>Consulta del reporte de ocupación de un evento (RF-06).</summary>
public sealed record OccupancyReportQuery(Guid EventId);

/// <summary>
/// Reporte de ocupación (RF-06). "Vendidas" e "ingresos" cuentan solo reservas
/// confirmadas; "disponibles" excluye las entradas perdidas por RN07 (ADR-004).
/// </summary>
public sealed record OccupancyReportDto(
    Guid EventId,
    string Title,
    int Capacity,
    int TicketsSold,
    int AvailableSeats,
    decimal OccupancyPercentage,
    decimal TotalRevenue,
    EventStatus Status);
