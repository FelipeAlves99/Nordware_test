using ProductReservation.Domain.Common;

namespace ProductReservation.Domain.Customers;

public sealed class Customer
{
    private Customer()
    {
        Name = string.Empty;
    }

    public Customer(Guid id, string name)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException("Customer identifier is required.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Customer name is required.");
        }

        Id = id;
        Name = name.Trim();
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }
}
