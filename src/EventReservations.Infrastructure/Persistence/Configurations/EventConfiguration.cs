using EventReservations.Domain.Common;
using EventReservations.Domain.Events;
using EventReservations.Domain.Venues;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventReservations.Infrastructure.Persistence.Configurations;

public sealed class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("events");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Title).IsRequired().HasMaxLength(Event.TitleMaxLength);
        builder.Property(e => e.Description).IsRequired().HasMaxLength(Event.DescriptionMaxLength);

        builder.Property(e => e.VenueId).IsRequired();
        builder.HasOne<Venue>()
               .WithMany()
               .HasForeignKey(e => e.VenueId)
               .OnDelete(DeleteBehavior.Restrict); // integridad referencial

        // Capacity (VO de un solo valor) -> conversión a int.
        builder.Property(e => e.Capacity)
               .HasConversion(c => c.Value, v => Capacity.Of(v))
               .HasColumnName("Capacity")
               .IsRequired();

        // Schedule (VO de varios campos) -> owned type. UTC de punta a punta:
        // timestamp with time zone (default de Npgsql para DateTime UTC).
        builder.OwnsOne(e => e.Schedule, s =>
        {
            s.Property(x => x.StartsAt).HasColumnName("StartsAt").IsRequired();
            s.Property(x => x.EndsAt).HasColumnName("EndsAt").IsRequired();
        });
        builder.Navigation(e => e.Schedule).IsRequired();

        builder.Property(e => e.Price)
               .HasConversion(m => m.Amount, v => Money.Of(v))
               .HasColumnType("numeric(10,2)")
               .IsRequired();

        builder.Property(e => e.Type).HasConversion<string>().IsRequired().HasMaxLength(20);
        builder.Property(e => e.Status).HasConversion<string>().IsRequired().HasMaxLength(20);

        builder.Property(e => e.SeatsTaken).IsRequired();
        builder.Property(e => e.LostSeats).IsRequired();

        // Propiedad calculada: no se persiste.
        builder.Ignore(e => e.AvailableSeats);

        // ADR-006: control de concurrencia optimista con la columna de sistema xmin.
        builder.Property<uint>("xmin").IsRowVersion().HasColumnName("xmin");
    }
}
