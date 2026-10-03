using Microsoft.EntityFrameworkCore;
using ProductReservation.Domain.Customers;
using ProductReservation.Domain.Products;
using ProductReservation.Domain.Reservations;

namespace ProductReservation.Application.Common.Interfaces;

public interface IAppDbContext
{
    DbSet<Customer> Customers { get; }

    DbSet<Product> Products { get; }

    DbSet<Reservation> Reservations { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
