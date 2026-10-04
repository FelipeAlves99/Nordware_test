namespace ProductReservation.Api;

internal static class ApiProblems
{
    public static IResult BadRequest(string code, string detail)
    {
        return Results.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Invalid request",
            detail: detail,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = code
            });
    }
}
