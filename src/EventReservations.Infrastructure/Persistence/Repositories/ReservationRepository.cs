using EventReservations.Application.Abstractions;
using EventReservations.Domain.Reservations;
using Microsoft.EntityFrameworkCore;

namespace EventReservations.Infrastructure.Persistence.Repositories;

public sealed class ReservationRepository : IReservationRepository
{
    private readonly AppDbContext _db;

    public ReservationRepository(AppDbContext db) => _db = db;

    public Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.Reservations.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task AddAsync(Reservation reservation, CancellationToken cancellationToken = default)
        => await _db.Reservations.AddAsync(reservation, cancellationToken);

    public Task<bool> ExistsCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var target = ReservationCode.From(code);
        return _db.Reservations.AnyAsync(r => r.Code == target, cancellationToken);
    }

    public async Task<IReadOnlyList<Reservation>> ListActiveByEventAsync(
        Guid eventId, CancellationToken cancellationToken = default)
        => await _db.Reservations
            .Where(r => r.EventId == eventId && r.Status != ReservationStatus.Cancelada)
            .ToListAsync(cancellationToken);
}
