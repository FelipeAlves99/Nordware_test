using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductReservation.Domain.Customers;

namespace ProductReservation.Infrastructure.Persistence.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(customer => customer.Id);
        builder.Property(customer => customer.Id).ValueGeneratedNever();
        builder.Property(customer => customer.Name).IsRequired().HasMaxLength(200);

        builder.HasData(
            new { Id = SeedData.CustomerAId, Name = "Cliente A" },
            new { Id = SeedData.CustomerBId, Name = "Cliente B" });
    }
}
