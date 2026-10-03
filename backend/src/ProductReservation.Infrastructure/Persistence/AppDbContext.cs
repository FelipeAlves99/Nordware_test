using Microsoft.EntityFrameworkCore;
using ProductReservation.Application.Common.Interfaces;
using ProductReservation.Domain.Customers;
using ProductReservation.Domain.Products;
using ProductReservation.Domain.Reservations;

namespace ProductReservation.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options), IAppDbContext
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<ProductStatus> ProductStatuses => Set<ProductStatus>();
    public DbSet<ReservationStatus> ReservationStatuses => Set<ReservationStatus>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

}
