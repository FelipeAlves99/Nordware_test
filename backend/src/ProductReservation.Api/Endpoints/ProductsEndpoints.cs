using MediatR;
using ProductReservation.Application.Products.ListProducts;

namespace ProductReservation.Api.Endpoints;

public static class ProductsEndpoints
{
    public static IEndpointRouteBuilder MapProductsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/products", async (ISender sender, CancellationToken cancellationToken) =>
        {
            var products = await sender.Send(new ListProductsQuery(), cancellationToken);
            return Results.Ok(products);
        })
        .WithName("ListProducts");

        return endpoints;
    }
}
