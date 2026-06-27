using EventReservations.Domain.Venues;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventReservations.Infrastructure.Persistence.Configurations;

public sealed class VenueConfiguration : IEntityTypeConfiguration<Venue>
{
    public void Configure(EntityTypeBuilder<Venue> builder)
    {
        builder.ToTable("venues");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).ValueGeneratedNever(); // datos de referencia con Id fijo

        builder.Property(v => v.Name).IsRequired().HasMaxLength(200);
        builder.Property(v => v.Capacity).IsRequired();
        builder.Property(v => v.City).HasMaxLength(100);

        // Seed: venues preexistentes del enunciado.
        builder.HasData(
            Venue.Create(1, "Auditorio Central", 200, "Bogotá"),
            Venue.Create(2, "Sala Norte", 50, "Bogotá"),
            Venue.Create(3, "Arena Sur", 500, "Medellín"));
    }
}
