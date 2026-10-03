using ProductReservation.Domain.Common;

namespace ProductReservation.Domain.Reservations;

public sealed class ReservationStatus : IEquatable<ReservationStatus>
{
    private ReservationStatus()
    {
    }

    private ReservationStatus(string id)
    {
        Id = id;
    }

    public static ReservationStatus Active { get; } = new("Active");

    public static ReservationStatus Cancelled { get; } = new("Cancelled");

    public static ReservationStatus Expired { get; } = new("Expired");

    public static ReservationStatus FromId(string id)
    {
        if (id == Active.Id)
        {
            return Active;
        }

        if (id == Cancelled.Id)
        {
            return Cancelled;
        }

        if (id == Expired.Id)
        {
            return Expired;
        }

        throw new DomainException($"Unknown reservation status '{id}'.");
    }

    public string Id { get; private set; } = string.Empty;

    public bool Equals(ReservationStatus? other)
    {
        return other is not null && Id == other.Id;
    }

    public override bool Equals(object? obj)
    {
        return obj is ReservationStatus other && Equals(other);
    }

    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(Id);
    }

    public override string ToString()
    {
        return Id;
    }
}
