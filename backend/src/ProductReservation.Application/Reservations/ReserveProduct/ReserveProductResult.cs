using ProductReservation.Domain.Reservations;

namespace ProductReservation.Application.Reservations.ReserveProduct;

public sealed record ReserveProductResult(
    Guid Id,
    Guid CustomerId,
    Guid ProductId,
    int Quantity,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset? CancelledAtUtc,
    DateTimeOffset? ExpiredAtUtc)
{
    public ReserveProductResult(Reservation reservation)
        : this(
            reservation.Id,
            reservation.CustomerId,
            reservation.ProductId,
            reservation.Quantity,
            reservation.Status.Id,
            reservation.CreatedAtUtc,
            reservation.ExpiresAtUtc,
            reservation.CancelledAtUtc,
            reservation.ExpiredAtUtc)
    {
    }
}
