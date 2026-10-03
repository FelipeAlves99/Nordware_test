using MediatR;
using Microsoft.EntityFrameworkCore;
using ProductReservation.Application.Common.Interfaces;
using ProductReservation.Domain.Reservations;

namespace ProductReservation.Application.Products.ListProducts;

public sealed class ListProductsQueryHandler(
    IAppDbContext dbContext,
    IProductLock productLock,
    TimeProvider timeProvider)
    : IRequestHandler<ListProductsQuery, IReadOnlyList<ListProductsResult>>
{
    public async Task<IReadOnlyList<ListProductsResult>> Handle(
        ListProductsQuery request,
        CancellationToken cancellationToken)
    {
        var products = await dbContext.Products
            .OrderBy(product => product.Name)
            .ToListAsync(cancellationToken);
        var result = new List<ListProductsResult>(products.Count);

        foreach (var product in products)
        {
            await using var productLockHandle = await productLock.AcquireAsync(product.Id, cancellationToken);

            var nowUtc = timeProvider.GetUtcNow();

            var activeReservations = await dbContext.Reservations
                .Where(reservation =>
                    reservation.ProductId == product.Id &&
                    reservation.StatusId == ReservationStatus.Active.Id &&
                    reservation.ExpiresAtUtc > nowUtc)
                .ToListAsync(cancellationToken);

            result.Add(new ListProductsResult(
                product,
                activeReservations.Sum(reservation => reservation.Quantity)));
        }

        return result;
    }
}
