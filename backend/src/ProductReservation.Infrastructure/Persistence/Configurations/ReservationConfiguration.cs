using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductReservation.Domain.Customers;
using ProductReservation.Domain.Products;
using ProductReservation.Domain.Reservations;

namespace ProductReservation.Infrastructure.Persistence.Configurations;

public sealed class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.HasKey(reservation => reservation.Id);
        builder.Property(reservation => reservation.Id).ValueGeneratedNever();
        builder.Property(reservation => reservation.CustomerId).IsRequired();
        builder.Property(reservation => reservation.ProductId).IsRequired();
        builder.Property(reservation => reservation.Quantity).IsRequired();
        builder.Property(reservation => reservation.CreatedAtUtc).IsRequired();
        builder.Property(reservation => reservation.ExpiresAtUtc).IsRequired();
        builder.Property(reservation => reservation.StatusId).IsRequired().HasMaxLength(32);
        builder.Property(reservation => reservation.CancelledAtUtc);
        builder.Property(reservation => reservation.ExpiredAtUtc);
        builder.Ignore(reservation => reservation.Status);

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(reservation => reservation.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(reservation => reservation.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ReservationStatus>()
            .WithMany()
            .HasForeignKey(reservation => reservation.StatusId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}
