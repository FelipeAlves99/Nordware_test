using ProductReservation.Application.Products.ListProducts;
using ProductReservation.Domain.Products;
using Xunit;

namespace ProductReservation.Application.Tests.Products;

public sealed class ListProductsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsProductsByNameAndCountsOnlyUnexpiredActiveReservations()
    {
        await using var dbContext = TestAppDbContext.Create();
        var productA = new Product("Produto A", 10);
        var productB = new Product("Produto B", 1);
        var customerId = Guid.NewGuid();
        var nowUtc = DateTimeOffset.UtcNow.AddHours(1);
        dbContext.Products.AddRange(productB, productA);
        TestReservations.Add(dbContext, customerId, productA.Id, 2);
        TestReservations.Add(
            dbContext,
            customerId,
            productA.Id,
            5,
            expiresAtUtc: nowUtc.AddTicks(-1));
        TestReservations.Add(
            dbContext,
            customerId,
            productA.Id,
            3,
            expiresAtUtc: nowUtc.AddHours(-2),
            expiredAtUtc: nowUtc.AddHours(-1));
        await dbContext.SaveChangesAsync();
        var handler = new ListProductsQueryHandler(
            dbContext,
            new TestProductLock(),
            new FixedTimeProvider(nowUtc));

        var result = await handler.Handle(new ListProductsQuery(), CancellationToken.None);

        Assert.Equal(new[] { "Produto A", "Produto B" }, result.Select(item => item.Name));
        var first = result[0];
        Assert.Equal(10, first.TotalQuantity);
        Assert.Equal(2, first.ReservedQuantity);
        Assert.Equal(8, first.AvailableQuantity);
        Assert.Equal("Reserved", first.Status);
        Assert.Equal(1, result[1].AvailableQuantity);
        Assert.Equal("Available", result[1].Status);
    }
}
