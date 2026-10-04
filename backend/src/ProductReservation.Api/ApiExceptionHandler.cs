using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ProductReservation.Application.Common.Exceptions;
using ProductReservation.Domain.Common;

namespace ProductReservation.Api;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, code, title, detail) = exception switch
        {
            ValidationException validationException => (
                StatusCodes.Status400BadRequest,
                "ValidationError",
                "Request validation failed",
                "One or more request fields are invalid."),
            ResourceNotFoundException notFoundException => (
                StatusCodes.Status404NotFound,
                $"{notFoundException.ResourceName}NotFound",
                "Resource not found",
                notFoundException.Message),
            BusinessConflictException conflictException => (
                StatusCodes.Status409Conflict,
                conflictException.Code,
                "Request conflict",
                conflictException.Message),
            ForbiddenException => (
                StatusCodes.Status400BadRequest,
                "InvalidRequest",
                "Invalid request",
                "The request could not be completed."),
            DomainException domainException => (
                StatusCodes.Status400BadRequest,
                "InvalidRequest",
                "Invalid request",
                domainException.Message),
            BadHttpRequestException badRequestException when badRequestException.StatusCode < 500 => (
                badRequestException.StatusCode,
                "MalformedRequest",
                "Malformed request",
                "The request body or route values are invalid."),
            _ => (
                StatusCodes.Status500InternalServerError,
                "InternalServerError",
                "An unexpected error occurred",
                "The request could not be completed.")
        };

        if (status >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception while processing an HTTP request.");
        }

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };
        problem.Extensions["code"] = code;

        if (exception is ValidationException validation)
        {
            problem.Extensions["errors"] = validation.Errors
                .GroupBy(failure => failure.PropertyName)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(failure => failure.ErrorMessage).ToArray());
        }

        httpContext.Response.StatusCode = status;
        httpContext.Response.ContentType = "application/problem+json";
        await JsonSerializer.SerializeAsync(httpContext.Response.Body, problem, JsonSerializerOptions.Web, cancellationToken);

        return true;
    }
}
