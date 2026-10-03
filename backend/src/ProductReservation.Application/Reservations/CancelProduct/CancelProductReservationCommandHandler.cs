using MediatR;
using Microsoft.EntityFrameworkCore;
using ProductReservation.Application.Common.Exceptions;
using ProductReservation.Application.Common.Interfaces;
using ProductReservation.Domain.Reservations;

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
        var customerExists = await dbContext.Customers
            .AnyAsync(customer => customer.Id == request.CustomerId, cancellationToken);

        if (!customerExists)
        {
            throw new ResourceNotFoundException("Customer", request.CustomerId);
        }

        var productExists = await dbContext.Products
            .AnyAsync(product => product.Id == request.ProductId, cancellationToken);

        if (!productExists)
        {
            throw new ResourceNotFoundException("Product", request.ProductId);
        }

        await using var productLockHandle = await productLock.AcquireAsync(request.ProductId, cancellationToken);

        var nowUtc = timeProvider.GetUtcNow();

        var reservation = await dbContext.Reservations
            .SingleOrDefaultAsync(item =>
                item.ProductId == request.ProductId &&
                item.CustomerId == request.CustomerId &&
                item.Status.Id == ReservationStatus.Active.Id,
                cancellationToken);

        reservation?.Cancel(nowUtc);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
