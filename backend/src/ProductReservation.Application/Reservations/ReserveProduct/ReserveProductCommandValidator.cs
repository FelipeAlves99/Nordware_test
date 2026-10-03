using FluentValidation;

namespace ProductReservation.Application.Reservations.ReserveProduct;

public sealed class ReserveProductCommandValidator : AbstractValidator<ReserveProductCommand>
{
    public ReserveProductCommandValidator()
    {
        RuleFor(command => command.ProductId).NotEmpty();
        RuleFor(command => command.CustomerId).NotEmpty();
        RuleFor(command => command.Quantity).GreaterThan(0);
    }
}
