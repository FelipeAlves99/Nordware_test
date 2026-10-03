using MediatR;
using ProductReservation.Application.Common.Exceptions;
using ProductReservation.Application.Reservations.CancelProduct;
using ProductReservation.Domain.Customers;
using ProductReservation.Domain.Products;
using ProductReservation.Domain.Reservations;
using Xunit;

namespace ProductReservation.Application.Tests.Reservations;

public sealed class CancelProductReservationCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenReservationIsActive_CancelsIt()
    {
        await using var dbContext = TestAppDbContext.Create();
        var customer = new Customer("Cliente A");
        var product = new Product("Produto A", 10);
        dbContext.Customers.Add(customer);
        dbContext.Products.Add(product);
        var reservation = TestReservations.Add(dbContext, customer.Id, product.Id, 3);
        await dbContext.SaveChangesAsync();
        var nowUtc = reservation.CreatedAtUtc.AddHours(1);
        var handler = CreateHandler(dbContext, nowUtc);

        var result = await handler.Handle(
            new CancelProductReservationCommand(product.Id, customer.Id),
            CancellationToken.None);

        Assert.Equal(Unit.Value, result);
        Assert.Equal("Cancelled", reservation.Status.Id);
        Assert.Equal(nowUtc, reservation.CancelledAtUtc);
    }

    [Fact]
    public async Task Handle_WhenReservationDoesNotExist_ReturnsSuccess()
    {
        await using var dbContext = TestAppDbContext.Create();
        var customer = new Customer("Cliente A");
        var product = new Product("Produto A", 10);
        dbContext.Customers.Add(customer);
        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();
        var handler = CreateHandler(dbContext, DateTimeOffset.UtcNow);

        var result = await handler.Handle(
            new CancelProductReservationCommand(product.Id, customer.Id),
            CancellationToken.None);

        Assert.Equal(Unit.Value, result);
    }

    [Fact]
    public async Task Handle_WhenReservationIsPastDue_LeavesItForExpirationWorker()
    {
        await using var dbContext = TestAppDbContext.Create();
        var customer = new Customer("Cliente A");
        var product = new Product("Produto A", 10);
        var nowUtc = DateTimeOffset.UtcNow.AddHours(1);
        dbContext.Customers.Add(customer);
        dbContext.Products.Add(product);
        var reservation = TestReservations.Add(
            dbContext,
            customer.Id,
            product.Id,
            3,
            expiresAtUtc: nowUtc.AddTicks(-1));
        await dbContext.SaveChangesAsync();
        var handler = CreateHandler(dbContext, nowUtc);

        await handler.Handle(
            new CancelProductReservationCommand(product.Id, customer.Id),
            CancellationToken.None);

        Assert.Equal("Active", reservation.Status.Id);
        Assert.Null(reservation.CancelledAtUtc);
        Assert.Null(reservation.ExpiredAtUtc);
    }

    [Fact]
    public async Task Handle_WhenCustomerDoesNotExist_ThrowsNotFound()
    {
        await using var dbContext = TestAppDbContext.Create();
        var product = new Product("Produto A", 10);
        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();
        var handler = CreateHandler(dbContext, DateTimeOffset.UtcNow);

        var exception = await Assert.ThrowsAsync<ResourceNotFoundException>(() => handler.Handle(
            new CancelProductReservationCommand(product.Id, Guid.NewGuid()),
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
        var handler = CreateHandler(dbContext, DateTimeOffset.UtcNow);

        var exception = await Assert.ThrowsAsync<ResourceNotFoundException>(() => handler.Handle(
            new CancelProductReservationCommand(Guid.NewGuid(), customer.Id),
            CancellationToken.None));

        Assert.Equal("Product", exception.ResourceName);
    }

    private static CancelProductReservationCommandHandler CreateHandler(
        TestAppDbContext dbContext,
        DateTimeOffset nowUtc)
    {
        return new CancelProductReservationCommandHandler(
            dbContext,
            new TestProductLock(),
            new FixedTimeProvider(nowUtc));
    }
}
