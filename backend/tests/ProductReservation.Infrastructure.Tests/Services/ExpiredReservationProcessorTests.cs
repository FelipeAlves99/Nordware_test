using Microsoft.EntityFrameworkCore;
using ProductReservation.Domain.Reservations;
using ProductReservation.Infrastructure.Persistence;
using ProductReservation.Infrastructure.Services;
using ProductReservation.Infrastructure.Workers;
using Xunit;

namespace ProductReservation.Infrastructure.Tests.Services;

public sealed class ExpiredReservationProcessorTests
{
    [Fact]
    public async Task ProcessBatch_ExpiresOnlyReservationsDueAtTheCapturedUtcTime()
    {
        var nowUtc = new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero);
        await using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureCreatedAsync();

        var expiredReservation = new Reservation(SeedData.CustomerAId, SeedData.ProductAId, 2);
        var futureReservation = new Reservation(SeedData.CustomerBId, SeedData.ProductAId, 1);
        var cancelledReservation = new Reservation(SeedData.CustomerAId, SeedData.ProductBId, 1);
        Assert.True(cancelledReservation.Cancel(cancelledReservation.CreatedAtUtc.AddMinutes(1)));

        SetExpiry(dbContext, expiredReservation, nowUtc);
        SetExpiry(dbContext, futureReservation, nowUtc.AddTicks(1));
        SetExpiry(dbContext, cancelledReservation, nowUtc.AddDays(-1));
        dbContext.Reservations.AddRange(expiredReservation, futureReservation, cancelledReservation);
        await dbContext.SaveChangesAsync();

        var processor = new ExpiredReservationProcessor(
            dbContext,
            new ProductLock(),
            new FixedTimeProvider(nowUtc));

        var processedCount = await processor.ProcessBatchAsync();

        Assert.Equal(1, processedCount);
        Assert.Equal(ReservationStatus.Expired.Id, expiredReservation.StatusId);
        Assert.Equal(nowUtc, expiredReservation.ExpiredAtUtc);
        Assert.Equal(ReservationStatus.Active.Id, futureReservation.StatusId);
        Assert.Null(futureReservation.ExpiredAtUtc);
        Assert.Equal(ReservationStatus.Cancelled.Id, cancelledReservation.StatusId);
        Assert.Null(cancelledReservation.ExpiredAtUtc);
        Assert.Equal(0, await processor.ProcessBatchAsync());
    }

    [Fact]
    public async Task ProcessBatch_RespectsBatchSizeAndCanContinueOnNextRun()
    {
        var nowUtc = new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero);
        await using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureCreatedAsync();

        var reservations = Enumerable.Range(0, 3)
            .Select(_ => new Reservation(SeedData.CustomerAId, SeedData.ProductAId, 1))
            .ToArray();
        foreach (var reservation in reservations)
        {
            SetExpiry(dbContext, reservation, nowUtc);
        }

        dbContext.Reservations.AddRange(reservations);
        await dbContext.SaveChangesAsync();
        var processor = new ExpiredReservationProcessor(
            dbContext,
            new ProductLock(),
            new FixedTimeProvider(nowUtc));

        Assert.Equal(2, await processor.ProcessBatchAsync(batchSize: 2));
        Assert.Equal(1, await processor.ProcessBatchAsync(batchSize: 2));
        Assert.All(reservations, reservation =>
        {
            Assert.Equal(ReservationStatus.Expired.Id, reservation.StatusId);
            Assert.Equal(nowUtc, reservation.ExpiredAtUtc);
        });
    }

    [Fact]
    public async Task ConcurrentProcessors_ExpireReservationOnlyOnce()
    {
        var databaseName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        var nowUtc = new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero);

        await using (var seedContext = new AppDbContext(options))
        {
            await seedContext.Database.EnsureCreatedAsync();
            var reservation = new Reservation(SeedData.CustomerAId, SeedData.ProductAId, 1);
            seedContext.Entry(reservation).Property(item => item.ExpiresAtUtc).CurrentValue = nowUtc;
            seedContext.Reservations.Add(reservation);
            await seedContext.SaveChangesAsync();
        }

        var productLock = new ProductLock();
        await using var firstContext = new AppDbContext(options);
        await using var secondContext = new AppDbContext(options);
        var firstProcessor = new ExpiredReservationProcessor(firstContext, productLock, new FixedTimeProvider(nowUtc));
        var secondProcessor = new ExpiredReservationProcessor(secondContext, productLock, new FixedTimeProvider(nowUtc));

        var results = await Task.WhenAll(
            firstProcessor.ProcessBatchAsync(),
            secondProcessor.ProcessBatchAsync());

        Assert.Equal(1, results.Sum());
        await using var verificationContext = new AppDbContext(options);
        var storedReservation = await verificationContext.Reservations.SingleAsync();
        Assert.Equal(ReservationStatus.Expired.Id, storedReservation.StatusId);
        Assert.Equal(nowUtc, storedReservation.ExpiredAtUtc);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static void SetExpiry(AppDbContext dbContext, Reservation reservation, DateTimeOffset expiresAtUtc)
    {
        dbContext.Entry(reservation).Property(item => item.ExpiresAtUtc).CurrentValue = expiresAtUtc;
    }

    private sealed class FixedTimeProvider(DateTimeOffset nowUtc) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => nowUtc;
    }
}
