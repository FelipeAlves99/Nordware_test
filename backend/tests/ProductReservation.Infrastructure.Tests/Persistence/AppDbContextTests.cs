using Microsoft.EntityFrameworkCore;
using ProductReservation.Domain.Reservations;
using ProductReservation.Infrastructure.Persistence;
using Xunit;

namespace ProductReservation.Infrastructure.Tests.Persistence;

public sealed class AppDbContextTests
{
    [Fact]
    public async Task EnsureCreated_SeedsStableCustomersProductsAndStatuses()
    {
        await using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureCreatedAsync();

        var customers = await dbContext.Customers.OrderBy(customer => customer.Name).ToListAsync();
        var products = await dbContext.Products.OrderBy(product => product.Name).ToListAsync();

        Assert.Collection(
            customers,
            customer =>
            {
                Assert.Equal(SeedData.CustomerAId, customer.Id);
                Assert.Equal("Cliente A", customer.Name);
            },
            customer =>
            {
                Assert.Equal(SeedData.CustomerBId, customer.Id);
                Assert.Equal("Cliente B", customer.Name);
            });
        Assert.Collection(
            products,
            product =>
            {
                Assert.Equal(SeedData.ProductAId, product.Id);
                Assert.Equal("Produto A", product.Name);
                Assert.Equal(10, product.TotalQuantity);
            },
            product =>
            {
                Assert.Equal(SeedData.ProductBId, product.Id);
                Assert.Equal("Produto B", product.Name);
                Assert.Equal(1, product.TotalQuantity);
            },
            product =>
            {
                Assert.Equal(SeedData.ProductCId, product.Id);
                Assert.Equal("Produto C", product.Name);
                Assert.Equal(0, product.TotalQuantity);
            });
        Assert.Equal(3, await dbContext.ProductStatuses.CountAsync());
        Assert.Equal(3, await dbContext.ReservationStatuses.CountAsync());
    }

    [Fact]
    public async Task ReservationStatusTransitions_ArePersistedAsLookupReferences()
    {
        await using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureCreatedAsync();
        var reservation = new Reservation(SeedData.CustomerAId, SeedData.ProductAId, 2);
        dbContext.Reservations.Add(reservation);

        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        var storedReservation = await dbContext.Reservations.SingleAsync(item => item.Id == reservation.Id);
        Assert.Equal(ReservationStatus.Active.Id, storedReservation.Status.Id);
        Assert.True(storedReservation.Cancel(storedReservation.CreatedAtUtc.AddMinutes(1)));
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        storedReservation = await dbContext.Reservations.SingleAsync(item => item.Id == reservation.Id);
        Assert.Equal(ReservationStatus.Cancelled.Id, storedReservation.Status.Id);
        Assert.NotNull(storedReservation.CancelledAtUtc);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
