using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProductReservation.Application.Common.Interfaces;
using ProductReservation.Infrastructure.Persistence;
using ProductReservation.Infrastructure.Services;

namespace ProductReservation.Infrastructure;

public static class DependencyInjection
{
    public const string DefaultDatabaseName = "ProductReservation";

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string databaseName = DefaultDatabaseName)
    {
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());
        services.AddSingleton<IProductLock, ProductLock>();

        return services;
    }
}
