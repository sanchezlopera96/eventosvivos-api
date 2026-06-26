using EventReservations.Domain.Venues;

namespace EventReservations.Application.Abstractions;

public interface IVenueRepository
{
    Task<Venue?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
