namespace ProductReservation.Domain.Products;

public sealed class ProductStatus : IEquatable<ProductStatus>
{
    private ProductStatus()
    {
    }

    private ProductStatus(string id)
    {
        Id = id;
    }

    public static ProductStatus Available { get; } = new("Available");

    public static ProductStatus Reserved { get; } = new("Reserved");

    public static ProductStatus Unavailable { get; } = new("Unavailable");

    public string Id { get; private set; } = string.Empty;

    public bool Equals(ProductStatus? other)
    {
        return other is not null && Id == other.Id;
    }

    public override bool Equals(object? obj)
    {
        return obj is ProductStatus other && Equals(other);
    }

    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(Id);
    }

    public override string ToString()
    {
        return Id;
    }
}
