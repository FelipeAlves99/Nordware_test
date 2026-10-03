using MediatR;

namespace ProductReservation.Application.Reservations.CancelProduct;

public sealed record CancelProductReservationCommand(Guid ProductId, Guid CustomerId) : IRequest<Unit>;
