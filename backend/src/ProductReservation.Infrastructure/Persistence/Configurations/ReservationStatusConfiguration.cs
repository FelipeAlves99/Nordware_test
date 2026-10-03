using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductReservation.Domain.Reservations;

namespace ProductReservation.Infrastructure.Persistence.Configurations;

public sealed class ReservationStatusConfiguration : IEntityTypeConfiguration<ReservationStatus>
{
    public void Configure(EntityTypeBuilder<ReservationStatus> builder)
    {
        builder.HasKey(status => status.Id);
        builder.Property(status => status.Id).ValueGeneratedNever().HasMaxLength(32);
        builder.HasData(
            new { Id = ReservationStatus.Active.Id },
            new { Id = ReservationStatus.Cancelled.Id },
            new { Id = ReservationStatus.Expired.Id });
    }
}
