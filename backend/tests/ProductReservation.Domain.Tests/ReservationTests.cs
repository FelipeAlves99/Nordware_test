using ProductReservation.Domain.Common;
using ProductReservation.Domain.Reservations;
using Xunit;

namespace ProductReservation.Domain.Tests;

public sealed class ReservationTests
{
    [Fact]
    public void Constructor_CreatesActiveReservationThatExpiresInSeventyTwoHours()
    {
        var reservation = CreateReservation();

        Assert.Equal("Active", reservation.Status.Id);
        Assert.Equal(reservation.CreatedAtUtc.AddHours(72), reservation.ExpiresAtUtc);
        Assert.True(reservation.IsActiveAt(reservation.CreatedAtUtc.AddHours(71)));
    }

    [Fact]
    public void TryExpire_AtExpiration_ExpiresReservationOnlyOnce()
    {
        var reservation = CreateReservation();
        var expirationTime = reservation.ExpiresAtUtc;

        var expired = reservation.TryExpire(expirationTime);

        Assert.True(expired);
        Assert.Equal("Expired", reservation.Status.Id);
        Assert.Equal(expirationTime, reservation.ExpiredAtUtc);
        Assert.False(reservation.TryExpire(expirationTime.AddMinutes(1)));
    }

    [Fact]
    public void Cancel_AfterExpiration_MarksReservationAsExpiredInsteadOfCancelled()
    {
        var reservation = CreateReservation();

        var cancelled = reservation.Cancel(reservation.ExpiresAtUtc);

        Assert.False(cancelled);
        Assert.Equal("Expired", reservation.Status.Id);
        Assert.Null(reservation.CancelledAtUtc);
    }

    [Fact]
    public void Cancel_ActiveReservation_IsIdempotent()
    {
        var reservation = CreateReservation();
        var cancellationTime = reservation.CreatedAtUtc.AddHours(1);

        Assert.True(reservation.Cancel(cancellationTime));
        Assert.Equal("Cancelled", reservation.Status.Id);
        Assert.Equal(cancellationTime, reservation.CancelledAtUtc);
        Assert.False(reservation.Cancel(cancellationTime.AddMinutes(1)));
    }

    [Fact]
    public void Constructor_WhenQuantityIsNotPositive_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => new Reservation(Guid.NewGuid(), Guid.NewGuid(), 0));
    }

    private static Reservation CreateReservation()
    {
        return new Reservation(Guid.NewGuid(), Guid.NewGuid(), 3);
    }
}
