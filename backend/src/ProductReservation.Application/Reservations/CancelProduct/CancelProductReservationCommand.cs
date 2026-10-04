using MediatR;

namespace ProductReservation.Application.Reservations.CancelProduct;

public sealed record CancelProductReservationCommand(Guid ReservationId, Guid CustomerId) : IRequest<Unit>;
