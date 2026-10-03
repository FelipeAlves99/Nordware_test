using MediatR;

namespace ProductReservation.Application.Reservations.ListCustomerReservations;

public sealed record ListCustomerReservationsQuery(Guid CustomerId) : IRequest<IReadOnlyList<ListCustomerReservationsResult>>;
