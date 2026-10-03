using MediatR;
using Microsoft.EntityFrameworkCore;
using ProductReservation.Application.Common.Exceptions;
using ProductReservation.Application.Common.Interfaces;
using ProductReservation.Domain.Reservations;

namespace ProductReservation.Application.Reservations.ReserveProduct;

public sealed class ReserveProductCommandHandler(
    IAppDbContext dbContext,
    IProductLock productLock,
    TimeProvider timeProvider)
    : IRequestHandler<ReserveProductCommand, ReserveProductResult>
{
    public async Task<ReserveProductResult> Handle(
        ReserveProductCommand request,
        CancellationToken cancellationToken)
    {
        var customerExists = await dbContext.Customers
            .AnyAsync(customer => customer.Id == request.CustomerId, cancellationToken);

        if (!customerExists)
        {
            throw new ResourceNotFoundException("Customer", request.CustomerId);
        }

        var product = await dbContext.Products
            .SingleOrDefaultAsync(item => item.Id == request.ProductId, cancellationToken);

        if (product is null)
        {
            throw new ResourceNotFoundException("Product", request.ProductId);
        }

        await using var productLockHandle = await productLock.AcquireAsync(product.Id, cancellationToken);

        var nowUtc = timeProvider.GetUtcNow();

        var activeForCustomer = await dbContext.Reservations
            .AnyAsync(reservation =>
                reservation.ProductId == product.Id &&
                reservation.CustomerId == request.CustomerId &&
                reservation.Status.Id == ReservationStatus.Active.Id &&
                reservation.ExpiresAtUtc > nowUtc,
                cancellationToken);

        if (activeForCustomer)
        {
            throw new BusinessConflictException(
                "ActiveReservationExists",
                "The customer already has an active reservation for this product.");
        }

        var activeReservations = await dbContext.Reservations
            .Where(reservation =>
                reservation.ProductId == product.Id &&
                reservation.Status.Id == ReservationStatus.Active.Id &&
                reservation.ExpiresAtUtc > nowUtc)
            .ToListAsync(cancellationToken);
        var reservedQuantity = activeReservations.Sum(reservation => reservation.Quantity);
        var availability = product.GetAvailability(reservedQuantity);

        if (request.Quantity > availability.AvailableQuantity)
        {
            throw new BusinessConflictException(
                "ProductUnavailable",
                "Product does not have enough available quantity.");
        }

        product.EnsureCanReserve(request.Quantity, reservedQuantity);

        var reservation = new Reservation(request.CustomerId, product.Id, request.Quantity);
        dbContext.Reservations.Add(reservation);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ReserveProductResult(reservation);
    }
}
