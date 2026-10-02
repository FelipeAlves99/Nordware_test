using System.Diagnostics.CodeAnalysis;
using ProductReservation.Domain.Common;

namespace ProductReservation.Domain.Reservations;

public sealed class Reservation
{
    private static readonly TimeSpan ReservationDuration = TimeSpan.FromHours(72);
    private Reservation()
    {
        Id = Guid.NewGuid();
        Status = ReservationStatus.Active;
    }

    [SetsRequiredMembers]
    public Reservation(Guid customerId, Guid productId, int quantity)
    {
        if (customerId == Guid.Empty)
        {
            throw new DomainException("Reservation customer identifier is required.");
        }

        if (productId == Guid.Empty)
        {
            throw new DomainException("Reservation product identifier is required.");
        }

        if (quantity <= 0)
        {
            throw new DomainException("Reservation quantity must be greater than zero.");
        }

        var now = DateTimeOffset.UtcNow;

        Id = Guid.NewGuid();
        CustomerId = customerId;
        ProductId = productId;
        Quantity = quantity;
        CreatedAtUtc = now;
        ExpiresAtUtc = now.Add(ReservationDuration);
        Status = ReservationStatus.Active;
    }

    public Guid Id { get; }

    public required Guid CustomerId { get; set; }

    public required Guid ProductId { get; set; }

    public required int Quantity { get; set; }

    public required DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public ReservationStatus Status { get; private set; }

    public DateTimeOffset? CancelledAtUtc { get; private set; }

    public DateTimeOffset? ExpiredAtUtc { get; private set; }

    public bool IsActiveAt(DateTimeOffset nowUtc)
    {
        return Status == ReservationStatus.Active && nowUtc < ExpiresAtUtc;
    }

    public bool TryExpire(DateTimeOffset nowUtc)
    {
        if (Status != ReservationStatus.Active || nowUtc < ExpiresAtUtc)
        {
            return false;
        }

        Status = ReservationStatus.Expired;
        ExpiredAtUtc = nowUtc;
        return true;
    }

    public bool Cancel(DateTimeOffset nowUtc)
    {
        if (Status != ReservationStatus.Active)
        {
            return false;
        }

        if (TryExpire(nowUtc))
        {
            return false;
        }

        Status = ReservationStatus.Cancelled;
        CancelledAtUtc = nowUtc;
        return true;
    }

}
