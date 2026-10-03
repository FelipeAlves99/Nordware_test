using ProductReservation.Infrastructure.Services;
using Xunit;

namespace ProductReservation.Infrastructure.Tests.Services;

public sealed class ProductLockTests
{
    [Fact]
    public async Task AcquireAsync_SerializesSameProductWithoutBlockingDifferentProducts()
    {
        var productLock = new ProductLock();
        var firstProductId = Guid.NewGuid();
        var secondProductId = Guid.NewGuid();
        var firstLease = await productLock.AcquireAsync(firstProductId);
        var waitingLeaseTask = productLock.AcquireAsync(firstProductId).AsTask();

        Assert.False(waitingLeaseTask.IsCompleted);

        await using var otherProductLease = await productLock
            .AcquireAsync(secondProductId)
            .AsTask()
            .WaitAsync(TimeSpan.FromSeconds(2));

        Assert.False(waitingLeaseTask.IsCompleted);

        await firstLease.DisposeAsync();
        await using var nextLease = await waitingLeaseTask.WaitAsync(TimeSpan.FromSeconds(2));
    }
}
