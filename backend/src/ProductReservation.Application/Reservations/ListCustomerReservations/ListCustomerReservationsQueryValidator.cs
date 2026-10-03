using FluentValidation;

namespace ProductReservation.Application.Reservations.ListCustomerReservations;

public sealed class ListCustomerReservationsQueryValidator : AbstractValidator<ListCustomerReservationsQuery>
{
    public ListCustomerReservationsQueryValidator()
    {
        RuleFor(query => query.CustomerId).NotEmpty();
    }
}
