namespace ProductReservation.Application.Common.Interfaces;

public interface IProductLock
{
    ValueTask<IAsyncDisposable> AcquireAsync(Guid productId, CancellationToken cancellationToken = default);
}
