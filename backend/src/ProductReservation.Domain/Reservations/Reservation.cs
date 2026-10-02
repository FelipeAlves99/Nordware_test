using ProductReservation.Domain.Common;

namespace ProductReservation.Domain.Reservations;

public sealed class Reservation
{
    private static readonly TimeSpan ReservationDuration = TimeSpan.FromHours(72);

    private Reservation()
    {
        Status = ReservationStatus.Active;
    }

    public Reservation(Guid id, Guid customerId, Guid productId, int quantity, DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException("Reservation identifier is required.");
        }

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

        EnsureUtc(createdAtUtc, nameof(createdAtUtc));

        Id = id;
        CustomerId = customerId;
        ProductId = productId;
        Quantity = quantity;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = createdAtUtc.Add(ReservationDuration);
        Status = ReservationStatus.Active;
    }

    public Guid Id { get; private set; }

    public Guid CustomerId { get; private set; }

    public Guid ProductId { get; private set; }

    public int Quantity { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public ReservationStatus Status { get; private set; }

    public DateTimeOffset? CancelledAtUtc { get; private set; }

    public DateTimeOffset? ExpiredAtUtc { get; private set; }

    public bool IsActiveAt(DateTimeOffset nowUtc)
    {
        EnsureUtc(nowUtc, nameof(nowUtc));
        return Status == ReservationStatus.Active && nowUtc < ExpiresAtUtc;
    }

    public bool TryExpire(DateTimeOffset nowUtc)
    {
        EnsureUtc(nowUtc, nameof(nowUtc));

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
        EnsureUtc(nowUtc, nameof(nowUtc));

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

    private static void EnsureUtc(DateTimeOffset value, string parameterName)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainException($"{parameterName} must be expressed in UTC.");
        }
    }
}
