using System.Diagnostics.CodeAnalysis;
using ProductReservation.Domain.Common;

namespace ProductReservation.Domain.Products;

public sealed class Product
{
    private Product()
    {
    }

    [SetsRequiredMembers]
    public Product(string name, int totalQuantity)
    {
        if (totalQuantity < 0)
        {
            throw new DomainException("Product total quantity cannot be negative.");
        }

        Id = Guid.NewGuid();
        Name = name;
        TotalQuantity = totalQuantity;
    }

    public Guid Id { get; }

    public required string Name { get; set; }

    public required int TotalQuantity { get; set; }

    public ProductAvailability GetAvailability(int reservedQuantity)
    {
        if (reservedQuantity < 0 || reservedQuantity > TotalQuantity)
        {
            throw new DomainException("Reserved quantity must be between zero and the product total quantity.");
        }

        return new ProductAvailability(TotalQuantity, reservedQuantity);
    }

    public void EnsureCanReserve(int requestedQuantity, int reservedQuantity)
    {
        if (requestedQuantity <= 0)
        {
            throw new DomainException("Reservation quantity must be greater than zero.");
        }

        var availability = GetAvailability(reservedQuantity);

        if (requestedQuantity > availability.AvailableQuantity)
        {
            throw new DomainException("Product does not have enough available quantity.");
        }
    }
}
