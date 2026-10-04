using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ProductReservation.Application.Common.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        logger.LogInformation("Handling {RequestName}.", requestName);

        try
        {
            var response = await next();
            stopwatch.Stop();
            logger.LogInformation(
                "Handled {RequestName} in {ElapsedMilliseconds} ms.",
                requestName,
                stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            logger.LogError(
                exception,
                "Handling {RequestName} failed after {ElapsedMilliseconds} ms.",
                requestName,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
