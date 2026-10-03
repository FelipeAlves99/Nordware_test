using MediatR;

namespace ProductReservation.Application.Products.ListProducts;

public sealed record ListProductsQuery : IRequest<IReadOnlyList<ListProductsResult>>;
