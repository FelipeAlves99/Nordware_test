using MediatR;
using Microsoft.EntityFrameworkCore;
using ProductReservation.Application.Common.Exceptions;
using ProductReservation.Application.Common.Interfaces;

namespace ProductReservation.Application.Reservations.ListCustomerReservations;

public sealed class ListCustomerReservationsQueryHandler(
    IAppDbContext dbContext)
    : IRequestHandler<ListCustomerReservationsQuery, IReadOnlyList<ListCustomerReservationsResult>>
{
    public async Task<IReadOnlyList<ListCustomerReservationsResult>> Handle(
        ListCustomerReservationsQuery request,
        CancellationToken cancellationToken)
    {
        var customerExists = await dbContext.Customers
            .AnyAsync(customer => customer.Id == request.CustomerId, cancellationToken);

        if (!customerExists)
        {
            throw new ResourceNotFoundException("Customer", request.CustomerId);
        }

        var reservations = await dbContext.Reservations
            .Where(reservation => reservation.CustomerId == request.CustomerId)
            .OrderByDescending(reservation => reservation.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return reservations.Select(reservation => new ListCustomerReservationsResult(reservation)).ToArray();
    }
}
