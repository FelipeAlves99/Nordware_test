using MediatR;
using Microsoft.EntityFrameworkCore;
using ProductReservation.Application.Common.Exceptions;
using ProductReservation.Application.Common.Interfaces;

namespace ProductReservation.Application.Reservations.CancelProduct;

public sealed class CancelProductReservationCommandHandler(
    IAppDbContext dbContext,
    IProductLock productLock,
    TimeProvider timeProvider)
    : IRequestHandler<CancelProductReservationCommand, Unit>
{
    public async Task<Unit> Handle(
        CancelProductReservationCommand request,
        CancellationToken cancellationToken)
    {
        var reservationDetails = await dbContext.Reservations
            .Where(reservation => reservation.Id == request.ReservationId)
            .Select(reservation => new
            {
                reservation.ProductId,
                reservation.CustomerId
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (reservationDetails is null)
        {
            return Unit.Value;
        }

        EnsureCustomerPermission(reservationDetails.CustomerId, request.CustomerId);

        await using var productLockHandle = await productLock.AcquireAsync(
            reservationDetails.ProductId,
            cancellationToken);

        var nowUtc = timeProvider.GetUtcNow();

        var reservation = await dbContext.Reservations
            .SingleOrDefaultAsync(item => item.Id == request.ReservationId,
                cancellationToken);

        if (reservation is not null)
        {
            EnsureCustomerPermission(reservation.CustomerId, request.CustomerId);
            reservation.Cancel(nowUtc);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }

    private static void EnsureCustomerPermission(Guid reservationCustomerId, Guid requestCustomerId)
    {
        if (reservationCustomerId != requestCustomerId)
        {
            throw new ForbiddenException("Customer id is not allowed to cancel this reservation.");
        }
    }
}
