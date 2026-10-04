using Microsoft.Extensions.Logging;
using Quartz;

namespace ProductReservation.Infrastructure.Workers;

[DisallowConcurrentExecution]
public sealed class ReservationExpirationJob(
    ExpiredReservationProcessor processor,
    ILogger<ReservationExpirationJob> logger) : IJob
{
    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var expiredCount = await processor.ProcessBatchAsync(cancellationToken);
            logger.LogInformation(
                "Reservation expiration job processed {ExpiredCount} expired reservations.",
                expiredCount);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The scheduler is stopping; already persisted batches remain valid and the next
            // execution will pick up any reservations that were not processed yet.
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reservation expiration job failed; Quartz will retry according to its trigger policy.");
            throw;
        }
    }
}
