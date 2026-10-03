using ProductReservation.Application.Common.Exceptions;
using ProductReservation.Application.Reservations.ReserveProduct;
using ProductReservation.Domain.Customers;
using ProductReservation.Domain.Products;
using ProductReservation.Domain.Reservations;
using Xunit;

namespace ProductReservation.Application.Tests.Reservations;

public sealed class ReserveProductCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenRequestIsValid_CreatesReservationForRequestedCustomerAndProduct()
    {
        await using var dbContext = TestAppDbContext.Create();
        var customer = new Customer("Cliente A");
        var product = new Product("Produto A", 10);
        dbContext.Customers.Add(customer);
        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();

        var handler = new ReserveProductCommandHandler(
            dbContext,
            new TestProductLock(),
            new FixedTimeProvider(DateTimeOffset.UtcNow));

        var result = await handler.Handle(
            new ReserveProductCommand(product.Id, customer.Id, 3),
            CancellationToken.None);

        var reservation = Assert.Single(dbContext.Reservations);
        Assert.Equal(reservation.Id, result.Id);
        Assert.Equal(customer.Id, result.CustomerId);
        Assert.Equal(product.Id, result.ProductId);
        Assert.Equal(3, reservation.Quantity);
        Assert.Equal("Active", result.Status);
    }

    [Fact]
    public async Task Handle_WhenCustomerDoesNotExist_ThrowsNotFound()
    {
        await using var dbContext = TestAppDbContext.Create();
        var product = new Product("Produto A", 10);
        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();
        var handler = CreateHandler(dbContext);

        var exception = await Assert.ThrowsAsync<ResourceNotFoundException>(() => handler.Handle(
            new ReserveProductCommand(product.Id, Guid.NewGuid(), 1),
            CancellationToken.None));

        Assert.Equal("Customer", exception.ResourceName);
    }

    [Fact]
    public async Task Handle_WhenProductDoesNotExist_ThrowsNotFound()
    {
        await using var dbContext = TestAppDbContext.Create();
        var customer = new Customer("Cliente A");
        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync();
        var handler = CreateHandler(dbContext);

        var exception = await Assert.ThrowsAsync<ResourceNotFoundException>(() => handler.Handle(
            new ReserveProductCommand(Guid.NewGuid(), customer.Id, 1),
            CancellationToken.None));

        Assert.Equal("Product", exception.ResourceName);
    }

    [Fact]
    public async Task Handle_WhenCustomerAlreadyHasUnexpiredReservation_ThrowsConflict()
    {
        await using var dbContext = TestAppDbContext.Create();
        var customer = new Customer("Cliente A");
        var product = new Product("Produto A", 10);
        dbContext.Customers.Add(customer);
        dbContext.Products.Add(product);
        TestReservations.Add(dbContext, customer.Id, product.Id, 1);
        await dbContext.SaveChangesAsync();
        var handler = CreateHandler(dbContext);

        var exception = await Assert.ThrowsAsync<BusinessConflictException>(() => handler.Handle(
            new ReserveProductCommand(product.Id, customer.Id, 1),
            CancellationToken.None));

        Assert.Equal("ActiveReservationExists", exception.Code);
    }

    [Fact]
    public async Task Handle_WhenQuantityExceedsAvailableStock_ThrowsConflict()
    {
        await using var dbContext = TestAppDbContext.Create();
        var customer = new Customer("Cliente A");
        var otherCustomer = new Customer("Cliente B");
        var product = new Product("Produto A", 5);
        dbContext.Customers.AddRange(customer, otherCustomer);
        dbContext.Products.Add(product);
        TestReservations.Add(dbContext, otherCustomer.Id, product.Id, 4);
        await dbContext.SaveChangesAsync();
        var handler = CreateHandler(dbContext);

        var exception = await Assert.ThrowsAsync<BusinessConflictException>(() => handler.Handle(
            new ReserveProductCommand(product.Id, customer.Id, 2),
            CancellationToken.None));

        Assert.Equal("ProductUnavailable", exception.Code);
    }

    [Fact]
    public async Task Handle_WhenExistingReservationIsPastDue_IgnoresItWithoutExpiringIt()
    {
        await using var dbContext = TestAppDbContext.Create();
        var customer = new Customer("Cliente A");
        var product = new Product("Produto A", 5);
        var nowUtc = DateTimeOffset.UtcNow.AddHours(1);
        dbContext.Customers.Add(customer);
        dbContext.Products.Add(product);
        var pastDueReservation = TestReservations.Add(
            dbContext,
            customer.Id,
            product.Id,
            5,
            expiresAtUtc: nowUtc.AddTicks(-1));
        await dbContext.SaveChangesAsync();
        var handler = new ReserveProductCommandHandler(
            dbContext,
            new TestProductLock(),
            new FixedTimeProvider(nowUtc));

        await handler.Handle(new ReserveProductCommand(product.Id, customer.Id, 5), CancellationToken.None);

        Assert.Equal("Active", pastDueReservation.Status.Id);
        Assert.Null(pastDueReservation.ExpiredAtUtc);
        Assert.Equal(2, dbContext.Reservations.Count());
    }

    private static ReserveProductCommandHandler CreateHandler(TestAppDbContext dbContext)
    {
        return new ReserveProductCommandHandler(
            dbContext,
            new TestProductLock(),
            new FixedTimeProvider(DateTimeOffset.UtcNow));
    }
}
