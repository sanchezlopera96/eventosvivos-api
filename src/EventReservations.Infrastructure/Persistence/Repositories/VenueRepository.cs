using EventReservations.Application.Abstractions;
using EventReservations.Domain.Venues;
using Microsoft.EntityFrameworkCore;

namespace EventReservations.Infrastructure.Persistence.Repositories;

public sealed class VenueRepository : IVenueRepository
{
    private readonly AppDbContext _db;

    public VenueRepository(AppDbContext db) => _db = db;

    public Task<Venue?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _db.Venues.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
}
