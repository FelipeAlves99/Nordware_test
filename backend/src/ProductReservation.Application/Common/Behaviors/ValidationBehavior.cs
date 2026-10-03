using FluentValidation;
using MediatR;

namespace ProductReservation.Application.Common.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);
            var failures = validators
                .Select(validator => validator.Validate(context))
                .SelectMany(result => result.Errors)
                .Where(failure => failure is not null)
                .ToArray();

            if (failures.Length > 0)
            {
                throw new ValidationException(failures);
            }
        }

        return await next();
    }
}
