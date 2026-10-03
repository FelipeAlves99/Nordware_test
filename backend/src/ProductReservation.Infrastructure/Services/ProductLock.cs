using System.Collections.Concurrent;
using ProductReservation.Application.Common.Interfaces;

namespace ProductReservation.Infrastructure.Services;

public sealed class ProductLock : IProductLock
{
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _locks = new();

    public async ValueTask<IAsyncDisposable> AcquireAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var semaphore = _locks.GetOrAdd(productId, static _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(cancellationToken);
        return new Releaser(semaphore);
    }

    private sealed class Releaser(SemaphoreSlim semaphore) : IAsyncDisposable
    {
        private SemaphoreSlim? _semaphore = semaphore;

        public ValueTask DisposeAsync()
        {
            Interlocked.Exchange(ref _semaphore, null)?.Release();
            return ValueTask.CompletedTask;
        }
    }
}
