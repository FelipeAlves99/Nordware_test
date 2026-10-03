using ProductReservation.Domain.Products;

namespace ProductReservation.Application.Products.ListProducts;

public sealed record ListProductsResult
{
    public ListProductsResult(Product product, int reservedQuantity)
    {
        var availability = product.GetAvailability(reservedQuantity);

        Id = product.Id;
        Name = product.Name;
        TotalQuantity = availability.TotalQuantity;
        ReservedQuantity = availability.ReservedQuantity;
        AvailableQuantity = availability.AvailableQuantity;
        Status = availability.Status.Id;
    }

    public Guid Id { get; }

    public string Name { get; }

    public int TotalQuantity { get; }

    public int ReservedQuantity { get; }

    public int AvailableQuantity { get; }

    public string Status { get; }
}
