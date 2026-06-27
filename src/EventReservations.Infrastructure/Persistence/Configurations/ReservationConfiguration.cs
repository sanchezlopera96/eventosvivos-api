using EventReservations.Domain.Events;
using EventReservations.Domain.Reservations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventReservations.Infrastructure.Persistence.Configurations;

public sealed class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("reservations");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.EventId).IsRequired();
        builder.HasOne<Event>()
               .WithMany()
               .HasForeignKey(r => r.EventId)
               .OnDelete(DeleteBehavior.Restrict); // integridad referencial

        // BuyerInfo (VO de varios campos) -> owned type.
        builder.OwnsOne(r => r.Buyer, b =>
        {
            b.Property(x => x.Name).HasColumnName("BuyerName").IsRequired().HasMaxLength(200);
            b.Property(x => x.Email).HasColumnName("BuyerEmail").IsRequired().HasMaxLength(320);
        });
        builder.Navigation(r => r.Buyer).IsRequired();

        builder.Property(r => r.Quantity).IsRequired();
        builder.Property(r => r.Status).HasConversion<string>().IsRequired().HasMaxLength(20);

        // ReservationCode (VO opcional) -> string. Único cuando no es null
        // (en PostgreSQL los NULL se consideran distintos, así que el índice
        // único permite varias reservas sin código).
        builder.Property(r => r.Code)
               .HasConversion(c => c!.Value, v => ReservationCode.From(v))
               .HasColumnName("Code")
               .HasMaxLength(9);
        builder.HasIndex(r => r.Code).IsUnique();

        // UTC de punta a punta -> timestamp with time zone (default Npgsql).
        builder.Property(r => r.CreatedAt).IsRequired();
        builder.Property(r => r.ConfirmedAt);
        builder.Property(r => r.CancelledAt);
    }
}
