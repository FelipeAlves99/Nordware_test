using MediatR;

namespace ProductReservation.Application.Reservations.ReserveProduct;

public sealed record ReserveProductCommand(Guid ProductId, Guid CustomerId, int Quantity) : IRequest<ReserveProductResult>;
