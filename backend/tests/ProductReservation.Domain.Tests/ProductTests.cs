using ProductReservation.Domain.Common;
using ProductReservation.Domain.Products;
using Xunit;

namespace ProductReservation.Domain.Tests;

public sealed class ProductTests
{
    [Fact]
    public void GetAvailability_WhenPartOfStockIsReserved_ReturnsReservedStatusAndRemainingQuantity()
    {
        var product = new Product(Guid.NewGuid(), "Produto A", 10);

        var availability = product.GetAvailability(3);

        Assert.Equal(10, availability.TotalQuantity);
        Assert.Equal(3, availability.ReservedQuantity);
        Assert.Equal(7, availability.AvailableQuantity);
        Assert.Equal(ProductStatus.Reserved, availability.Status);
    }

    [Theory]
    [InlineData(10, 0, ProductStatus.Available)]
    [InlineData(10, 10, ProductStatus.Unavailable)]
    [InlineData(0, 0, ProductStatus.Unavailable)]
    public void GetAvailability_DerivesExpectedStatus(int totalQuantity, int reservedQuantity, ProductStatus expectedStatus)
    {
        var product = new Product(Guid.NewGuid(), "Produto", totalQuantity);

        var availability = product.GetAvailability(reservedQuantity);

        Assert.Equal(expectedStatus, availability.Status);
    }

    [Fact]
    public void EnsureCanReserve_WhenRequestedQuantityExceedsAvailability_ThrowsDomainException()
    {
        var product = new Product(Guid.NewGuid(), "Produto A", 10);

        Assert.Throws<DomainException>(() => product.EnsureCanReserve(8, 3));
    }

    [Fact]
    public void Constructor_WhenTotalQuantityIsNegative_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => new Product(Guid.NewGuid(), "Produto A", -1));
    }
}
