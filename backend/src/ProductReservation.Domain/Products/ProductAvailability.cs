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

    public ProductStatus Status => AvailableQuantity == 0
        ? ProductStatus.Unavailable
        : ReservedQuantity == 0
            ? ProductStatus.Available
            : ProductStatus.Reserved;
}
