using MediatR;
using ProductReservation.Application.Reservations.CancelProduct;
using ProductReservation.Application.Reservations.ListCustomerReservations;
using ProductReservation.Application.Reservations.ReserveProduct;

namespace ProductReservation.Api.Endpoints;

public static class ReservationsEndpoints
{
    public const string CustomerIdHeader = "X-Customer-Id";

    public static IEndpointRouteBuilder MapReservationsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/products/{productId}/reserve", ReserveProductAsync)
            .WithName("ReserveProduct");
        endpoints.MapDelete("/reservations/{reservationId}", CancelReservationAsync)
            .WithName("CancelReservation");
        endpoints.MapGet("/customer/{customerId}/reservations", ListCustomerReservationsAsync)
            .WithName("ListCustomerReservations");

        return endpoints;
    }

    private static async Task<IResult> ReserveProductAsync(
        string productId,
        ReserveProductCommand command,
        HttpRequest httpRequest,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(productId, out var parsedProductId) || parsedProductId == Guid.Empty)
        {
            return ApiProblems.BadRequest("InvalidProductId", "Product id must be a non-empty GUID.");
        }

        if (!TryGetCustomerId(httpRequest, out var customerId, out var problem))
        {
            return problem;
        }

        var result = await sender.Send(
            command with { ProductId = parsedProductId, CustomerId = customerId },
            cancellationToken);

        return Results.Json(result, statusCode: StatusCodes.Status201Created);
    }

    private static async Task<IResult> CancelReservationAsync(
        string reservationId,
        HttpRequest httpRequest,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(reservationId, out var parsedReservationId) || parsedReservationId == Guid.Empty)
        {
            return ApiProblems.BadRequest("InvalidReservationId", "Reservation id must be a non-empty GUID.");
        }

        if (!TryGetCustomerId(httpRequest, out var customerId, out var problem))
        {
            return problem;
        }

        await sender.Send(
            new CancelProductReservationCommand(parsedReservationId, customerId),
            cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> ListCustomerReservationsAsync(
        string customerId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(customerId, out var parsedCustomerId) || parsedCustomerId == Guid.Empty)
        {
            return ApiProblems.BadRequest("InvalidCustomerId", "Customer id must be a non-empty GUID.");
        }

        var results = await sender.Send(new ListCustomerReservationsQuery(parsedCustomerId), cancellationToken);
        return Results.Ok(results);
    }

    private static bool TryGetCustomerId(
        HttpRequest request,
        out Guid customerId,
        out IResult problem)
    {
        if (!request.Headers.TryGetValue(CustomerIdHeader, out var headerValue) || string.IsNullOrWhiteSpace(headerValue))
        {
            customerId = Guid.Empty;
            problem = ApiProblems.BadRequest("CustomerIdRequired", $"Header '{CustomerIdHeader}' is required.");
            return false;
        }

        if (!Guid.TryParse(headerValue, out customerId) || customerId == Guid.Empty)
        {
            problem = ApiProblems.BadRequest("InvalidCustomerId", $"Header '{CustomerIdHeader}' must contain a non-empty GUID.");
            return false;
        }

        problem = Results.Empty;
        return true;
    }
}
