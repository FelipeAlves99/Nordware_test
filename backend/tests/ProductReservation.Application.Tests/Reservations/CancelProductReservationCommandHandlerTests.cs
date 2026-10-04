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
            new CancelProductReservationCommand(reservation.Id, customer.Id),
            CancellationToken.None);

        Assert.Equal(Unit.Value, result);
        Assert.Equal("Cancelled", reservation.Status.Id);
        Assert.Equal(nowUtc, reservation.CancelledAtUtc);
    }

    [Fact]
    public async Task Handle_WhenReservationDoesNotExist_ReturnsSuccess()
    {
        await using var dbContext = TestAppDbContext.Create();
        var handler = CreateHandler(dbContext, DateTimeOffset.UtcNow);

        var result = await handler.Handle(
            new CancelProductReservationCommand(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        Assert.Equal(Unit.Value, result);
    }

    [Fact]
    public async Task Handle_WhenCustomerHasMultipleReservations_CancelsOnlyReservationById()
    {
        await using var dbContext = TestAppDbContext.Create();
        var customer = new Customer("Cliente A");
        var product = new Product("Produto A", 10);
        dbContext.Customers.Add(customer);
        dbContext.Products.Add(product);
        var firstReservation = TestReservations.Add(dbContext, customer.Id, product.Id, 2);
        var secondReservation = TestReservations.Add(dbContext, customer.Id, product.Id, 3);
        await dbContext.SaveChangesAsync();
        var handler = CreateHandler(dbContext, firstReservation.CreatedAtUtc.AddHours(1));

        await handler.Handle(
            new CancelProductReservationCommand(firstReservation.Id, customer.Id),
            CancellationToken.None);

        Assert.Equal("Cancelled", firstReservation.Status.Id);
        Assert.NotNull(firstReservation.CancelledAtUtc);
        Assert.Equal("Active", secondReservation.Status.Id);
        Assert.Null(secondReservation.CancelledAtUtc);
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
            new CancelProductReservationCommand(reservation.Id, customer.Id),
            CancellationToken.None);

        Assert.Equal("Active", reservation.Status.Id);
        Assert.Null(reservation.CancelledAtUtc);
        Assert.Null(reservation.ExpiredAtUtc);
    }

    [Fact]
    public async Task Handle_WhenCustomerDoesNotOwnReservation_ThrowsForbiddenAndLeavesItUnchanged()
    {
        await using var dbContext = TestAppDbContext.Create();
        var customer = new Customer("Cliente A");
        var product = new Product("Produto A", 10);
        dbContext.Customers.Add(customer);
        dbContext.Products.Add(product);
        var reservation = TestReservations.Add(dbContext, customer.Id, product.Id, 3);
        await dbContext.SaveChangesAsync();
        var handler = CreateHandler(dbContext, reservation.CreatedAtUtc.AddHours(1));

        var exception = await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(
            new CancelProductReservationCommand(reservation.Id, Guid.NewGuid()),
            CancellationToken.None));

        Assert.Equal("Customer id is not allowed to cancel this reservation.", exception.Message);
        Assert.Equal("Active", reservation.Status.Id);
        Assert.Null(reservation.CancelledAtUtc);
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
