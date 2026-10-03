using ProductReservation.Application.Common.Exceptions;
using ProductReservation.Application.Reservations.ListCustomerReservations;
using ProductReservation.Domain.Customers;
using Xunit;

namespace ProductReservation.Application.Tests.Reservations;

public sealed class ListCustomerReservationsQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenCustomerExists_ReturnsAllStatusesInCreationOrderWithoutExpiringThem()
    {
        await using var dbContext = TestAppDbContext.Create();
        var customer = new Customer("Cliente A");
        var productId = Guid.NewGuid();
        var nowUtc = DateTimeOffset.UtcNow.AddHours(1);
        dbContext.Customers.Add(customer);
        var activePastDue = TestReservations.Add(
            dbContext,
            customer.Id,
            productId,
            1,
            expiresAtUtc: nowUtc.AddTicks(-1));
        var cancelled = TestReservations.Add(
            dbContext,
            customer.Id,
            productId,
            2,
            cancelledAtUtc: DateTimeOffset.UtcNow.AddMinutes(1));
        var expired = TestReservations.Add(
            dbContext,
            customer.Id,
            productId,
            3,
            expiresAtUtc: nowUtc.AddHours(-2),
            expiredAtUtc: nowUtc.AddHours(-1));

        dbContext.Entry(activePastDue).Property(item => item.CreatedAtUtc).CurrentValue = nowUtc.AddHours(-3);
        dbContext.Entry(cancelled).Property(item => item.CreatedAtUtc).CurrentValue = nowUtc.AddHours(-2);
        dbContext.Entry(expired).Property(item => item.CreatedAtUtc).CurrentValue = nowUtc.AddHours(-1);
        await dbContext.SaveChangesAsync();
        var handler = new ListCustomerReservationsQueryHandler(dbContext);

        var result = await handler.Handle(
            new ListCustomerReservationsQuery(customer.Id),
            CancellationToken.None);

        Assert.Equal(new[] { expired.Id, cancelled.Id, activePastDue.Id }, result.Select(item => item.Id));
        Assert.Equal(new[] { "Expired", "Cancelled", "Active" }, result.Select(item => item.Status));
        Assert.Equal("Active", activePastDue.Status.Id);
        Assert.Null(activePastDue.ExpiredAtUtc);
    }

    [Fact]
    public async Task Handle_WhenCustomerDoesNotExist_ThrowsNotFound()
    {
        await using var dbContext = TestAppDbContext.Create();
        var handler = new ListCustomerReservationsQueryHandler(dbContext);

        var exception = await Assert.ThrowsAsync<ResourceNotFoundException>(() => handler.Handle(
            new ListCustomerReservationsQuery(Guid.NewGuid()),
            CancellationToken.None));

        Assert.Equal("Customer", exception.ResourceName);
    }
}
