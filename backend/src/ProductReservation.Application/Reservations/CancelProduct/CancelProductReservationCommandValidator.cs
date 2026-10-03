using FluentValidation;

namespace ProductReservation.Application.Reservations.CancelProduct;

public sealed class CancelProductReservationCommandValidator : AbstractValidator<CancelProductReservationCommand>
{
    public CancelProductReservationCommandValidator()
    {
        RuleFor(command => command.ProductId).NotEmpty();
        RuleFor(command => command.CustomerId).NotEmpty();
    }
}
