using EventReservations.Domain.Events;
using EventReservations.Domain.Reservations;
using EventReservations.Domain.Venues;
using Microsoft.EntityFrameworkCore;

namespace EventReservations.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Event> Events => Set<Event>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<Venue> Venues => Set<Venue>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
