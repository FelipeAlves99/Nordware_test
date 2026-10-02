using ProductReservation.Domain.Common;
using ProductReservation.Domain.Reservations;
using Xunit;

namespace ProductReservation.Domain.Tests;

public sealed class ReservationTests
{
    private static readonly DateTimeOffset CreatedAtUtc = new(2026, 10, 2, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_CreatesActiveReservationThatExpiresInSeventyTwoHours()
    {
        var reservation = CreateReservation();

        Assert.Equal(ReservationStatus.Active, reservation.Status);
        Assert.Equal(CreatedAtUtc.AddHours(72), reservation.ExpiresAtUtc);
        Assert.True(reservation.IsActiveAt(CreatedAtUtc.AddHours(71)));
    }

    [Fact]
    public void TryExpire_AtExpiration_ExpiresReservationOnlyOnce()
    {
        var reservation = CreateReservation();
        var expirationTime = CreatedAtUtc.AddHours(72);

        var expired = reservation.TryExpire(expirationTime);

        Assert.True(expired);
        Assert.Equal(ReservationStatus.Expired, reservation.Status);
        Assert.Equal(expirationTime, reservation.ExpiredAtUtc);
        Assert.False(reservation.TryExpire(expirationTime.AddMinutes(1)));
    }

    [Fact]
    public void Cancel_AfterExpiration_MarksReservationAsExpiredInsteadOfCancelled()
    {
        var reservation = CreateReservation();

        var cancelled = reservation.Cancel(CreatedAtUtc.AddHours(72));

        Assert.False(cancelled);
        Assert.Equal(ReservationStatus.Expired, reservation.Status);
        Assert.Null(reservation.CancelledAtUtc);
    }

    [Fact]
    public void Cancel_ActiveReservation_IsIdempotent()
    {
        var reservation = CreateReservation();
        var cancellationTime = CreatedAtUtc.AddHours(1);

        Assert.True(reservation.Cancel(cancellationTime));
        Assert.Equal(ReservationStatus.Cancelled, reservation.Status);
        Assert.Equal(cancellationTime, reservation.CancelledAtUtc);
        Assert.False(reservation.Cancel(cancellationTime.AddMinutes(1)));
    }

    [Fact]
    public void Constructor_WhenQuantityIsNotPositive_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => new Reservation(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            0,
            CreatedAtUtc));
    }

    private static Reservation CreateReservation()
    {
        return new Reservation(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            3,
            CreatedAtUtc);
    }
}
