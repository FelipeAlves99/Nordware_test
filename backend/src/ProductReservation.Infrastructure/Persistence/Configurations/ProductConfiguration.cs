using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductReservation.Domain.Products;

namespace ProductReservation.Infrastructure.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(product => product.Id);
        builder.Property(product => product.Id).ValueGeneratedNever();
        builder.Property(product => product.Name).IsRequired().HasMaxLength(200);
        builder.Property(product => product.TotalQuantity).IsRequired();

        builder.HasData(
            new { Id = SeedData.ProductAId, Name = "Produto A", TotalQuantity = 10 },
            new { Id = SeedData.ProductBId, Name = "Produto B", TotalQuantity = 1 },
            new { Id = SeedData.ProductCId, Name = "Produto C", TotalQuantity = 0 });
    }
}
