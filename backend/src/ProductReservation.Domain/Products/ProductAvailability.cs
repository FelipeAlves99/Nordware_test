namespace ProductReservation.Domain.Products;

public sealed class ProductAvailability
{
    internal ProductAvailability(int totalQuantity, int reservedQuantity)
    {
        TotalQuantity = totalQuantity;
        ReservedQuantity = reservedQuantity;
    }

    public int TotalQuantity { get; }

    public int ReservedQuantity { get; }

    public int AvailableQuantity => TotalQuantity - ReservedQuantity;

    public ProductStatus Status => TotalQuantity == 0
        ? ProductStatus.Unavailable
        : AvailableQuantity == 0
            ? ProductStatus.Reserved
            : ProductStatus.Available;
}
