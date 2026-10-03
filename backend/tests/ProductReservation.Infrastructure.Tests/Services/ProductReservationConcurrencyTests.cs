using Microsoft.EntityFrameworkCore;
using ProductReservation.Application.Common.Exceptions;
using ProductReservation.Application.Reservations.ReserveProduct;
using ProductReservation.Infrastructure.Persistence;
using ProductReservation.Infrastructure.Services;
using Xunit;

namespace ProductReservation.Infrastructure.Tests.Services;

public sealed class ProductReservationConcurrencyTests
{
    [Fact]
    public async Task ConcurrentReservationsForSameProduct_DoNotExceedStock()
    {
        var databaseName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        await using (var seedContext = new AppDbContext(options))
        {
            await seedContext.Database.EnsureCreatedAsync();
        }

        var productLock = new ProductLock();
        await using var firstContext = new AppDbContext(options);
        await using var secondContext = new AppDbContext(options);
        var firstHandler = new ReserveProductCommandHandler(firstContext, productLock, TimeProvider.System);
        var secondHandler = new ReserveProductCommandHandler(secondContext, productLock, TimeProvider.System);
        var firstRequest = new ReserveProductCommand(SeedData.ProductAId, SeedData.CustomerAId, 6);
        var secondRequest = new ReserveProductCommand(SeedData.ProductAId, SeedData.CustomerBId, 6);

        var results = await Task.WhenAll(
            TryReserveAsync(firstHandler, firstRequest),
            TryReserveAsync(secondHandler, secondRequest));

        Assert.Single(results, result => result);
        await using var verificationContext = new AppDbContext(options);
        var activeQuantity = await verificationContext.Reservations
            .Where(reservation => reservation.ProductId == SeedData.ProductAId)
            .SumAsync(reservation => reservation.Quantity);
        Assert.Equal(6, activeQuantity);
    }

    private static async Task<bool> TryReserveAsync(
        ReserveProductCommandHandler handler,
        ReserveProductCommand request)
    {
        try
        {
            await handler.Handle(request, CancellationToken.None);
            return true;
        }
        catch (BusinessConflictException)
        {
            return false;
        }
    }
}
