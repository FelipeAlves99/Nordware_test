using System.Diagnostics.CodeAnalysis;

namespace ProductReservation.Domain.Customers;

public sealed class Customer
{
    private Customer()
    {
    }

    [SetsRequiredMembers]
    public Customer(string name)
    {
        Id = Guid.NewGuid();
        Name = name;
    }

    public Guid Id { get; }

    public required string Name { get; set; }
}
