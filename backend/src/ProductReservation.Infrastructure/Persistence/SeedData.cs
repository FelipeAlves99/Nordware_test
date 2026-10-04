using Microsoft.EntityFrameworkCore;
using ProductReservation.Domain.Reservations;

namespace ProductReservation.Infrastructure.Persistence;

public static class SeedData
{
    public static readonly Guid CustomerAId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static readonly Guid CustomerBId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public static readonly Guid ProductAId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    public static readonly Guid ProductBId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    public static readonly Guid ProductCId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    public static readonly Guid ReservationToExpireId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    public static async Task InitializeAsync(
        AppDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken = default)
    {
        if (await dbContext.Reservations.AnyAsync(
                reservation => reservation.Id == ReservationToExpireId,
                cancellationToken))
        {
            return;
        }

        var nowUtc = timeProvider.GetUtcNow();
        // Align the demo reservation with the next run of Quartz's once-per-minute trigger.
        var nextMinuteUtc = new DateTimeOffset(
            nowUtc.Year,
            nowUtc.Month,
            nowUtc.Day,
            nowUtc.Hour,
            nowUtc.Minute,
            0,
            TimeSpan.Zero).AddMinutes(1);
        var reservation = new Reservation(CustomerAId, ProductAId, quantity: 1);
        var entry = dbContext.Reservations.Add(reservation);
        entry.Property(item => item.Id).CurrentValue = ReservationToExpireId;
        entry.Property(item => item.CreatedAtUtc).CurrentValue = nowUtc;
        entry.Property(item => item.ExpiresAtUtc).CurrentValue = nextMinuteUtc;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
