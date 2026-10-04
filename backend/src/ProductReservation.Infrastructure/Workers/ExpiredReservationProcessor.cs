using Microsoft.EntityFrameworkCore;
using ProductReservation.Application.Common.Interfaces;
using ProductReservation.Domain.Reservations;

namespace ProductReservation.Infrastructure.Workers;

public sealed class ExpiredReservationProcessor(
    IAppDbContext dbContext,
    IProductLock productLock,
    TimeProvider timeProvider)
{
    public const int DefaultBatchSize = 100;

    public async Task<int> ProcessBatchAsync(
        CancellationToken cancellationToken = default,
        int batchSize = DefaultBatchSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);

        // Bulk-update alternative for a relational provider (ExecuteUpdate is not supported
        // by the current EF Core InMemory provider). Keep the active-status predicate so a
        // reservation changed by another operation is not overwritten. Before replacing the
        // current implementation, preserve coordination with operations using ProductLock.
        // var now = timeProvider.GetUtcNow();
        // return await dbContext.Reservations
        //     .Where(reservation => reservation.StatusId == ReservationStatus.Active.Id
        //         && reservation.ExpiresAtUtc <= now)
        //     .ExecuteUpdateAsync(update => update
        //         .SetProperty(reservation => reservation.StatusId, ReservationStatus.Expired.Id)
        //         .SetProperty(reservation => reservation.ExpiredAtUtc, now), cancellationToken);

        var nowUtc = timeProvider.GetUtcNow();
        var candidates = await dbContext.Reservations
            .Where(reservation =>
                reservation.StatusId == ReservationStatus.Active.Id &&
                reservation.ExpiresAtUtc <= nowUtc)
            .OrderBy(reservation => reservation.ExpiresAtUtc)
            .Select(reservation => new ExpirationCandidate(reservation.ProductId))
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        var processedCount = 0;
        var productIds = candidates
            .Select(candidate => candidate.ProductId)
            .Distinct()
            .OrderBy(productId => productId);

        foreach (var productId in productIds)
        {
            if (processedCount == batchSize)
            {
                break;
            }

            await using var productLockHandle = await productLock.AcquireAsync(productId, cancellationToken);

            var dueReservations = await dbContext.Reservations
                .Where(reservation =>
                    reservation.ProductId == productId &&
                    reservation.StatusId == ReservationStatus.Active.Id &&
                    reservation.ExpiresAtUtc <= nowUtc)
                .OrderBy(reservation => reservation.ExpiresAtUtc)
                .Take(batchSize - processedCount)
                .ToListAsync(cancellationToken);

            var changedCount = 0;
            foreach (var reservation in dueReservations)
            {
                if (reservation.TryExpire(nowUtc))
                {
                    changedCount++;
                }
            }

            if (changedCount > 0)
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                processedCount += changedCount;
            }
        }

        return processedCount;
    }

    private sealed record ExpirationCandidate(Guid ProductId);
}
