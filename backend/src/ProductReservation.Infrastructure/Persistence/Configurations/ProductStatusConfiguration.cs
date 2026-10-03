using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductReservation.Domain.Products;

namespace ProductReservation.Infrastructure.Persistence.Configurations;

public sealed class ProductStatusConfiguration : IEntityTypeConfiguration<ProductStatus>
{
    public void Configure(EntityTypeBuilder<ProductStatus> builder)
    {
        builder.HasKey(status => status.Id);
        builder.Property(status => status.Id).ValueGeneratedNever().HasMaxLength(32);
        builder.HasData(
            new { Id = ProductStatus.Available.Id },
            new { Id = ProductStatus.Reserved.Id },
            new { Id = ProductStatus.Unavailable.Id });
    }
}
