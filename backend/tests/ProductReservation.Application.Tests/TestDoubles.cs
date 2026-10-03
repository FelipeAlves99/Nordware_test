using Microsoft.EntityFrameworkCore;
using ProductReservation.Application.Common.Interfaces;
using ProductReservation.Domain.Customers;
using ProductReservation.Domain.Products;
using ProductReservation.Domain.Reservations;

namespace ProductReservation.Application.Tests;

internal sealed class TestAppDbContext(DbContextOptions<TestAppDbContext> options)
    : DbContext(options), IAppDbContext
{
    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Reservation> Reservations => Set<Reservation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>().HasKey(customer => customer.Id);
        modelBuilder.Entity<Product>().HasKey(product => product.Id);
        modelBuilder.Entity<Reservation>().HasKey(reservation => reservation.Id);
        modelBuilder.Entity<ReservationStatus>().HasKey(status => status.Id);
        modelBuilder.Entity<Reservation>().Ignore(reservation => reservation.Status);
        modelBuilder.Entity<Reservation>()
            .HasOne<ReservationStatus>()
            .WithMany()
            .HasForeignKey(reservation => reservation.StatusId);
    }

    public static TestAppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<TestAppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TestAppDbContext(options);
    }
}

internal sealed class TestProductLock : IProductLock
{
    public ValueTask<IAsyncDisposable> AcquireAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult<IAsyncDisposable>(NoOpAsyncDisposable.Instance);
    }
}

internal sealed class FixedTimeProvider(DateTimeOffset nowUtc) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => nowUtc;
}

internal static class TestReservations
{
    public static Reservation Add(
        TestAppDbContext dbContext,
        Guid customerId,
        Guid productId,
        int quantity,
        DateTimeOffset? expiresAtUtc = null,
        DateTimeOffset? expiredAtUtc = null,
        DateTimeOffset? cancelledAtUtc = null)
    {
        var reservation = new Reservation(customerId, productId, quantity);

        if (expiresAtUtc is not null)
        {
            dbContext.Entry(reservation).Property(item => item.ExpiresAtUtc).CurrentValue = expiresAtUtc.Value;
        }

        if (expiredAtUtc is not null)
        {
            reservation.TryExpire(expiredAtUtc.Value);
        }

        if (cancelledAtUtc is not null)
        {
            reservation.Cancel(cancelledAtUtc.Value);
        }

        dbContext.Reservations.Add(reservation);
        return reservation;
    }
}

internal sealed class NoOpAsyncDisposable : IAsyncDisposable
{
    public static NoOpAsyncDisposable Instance { get; } = new();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
