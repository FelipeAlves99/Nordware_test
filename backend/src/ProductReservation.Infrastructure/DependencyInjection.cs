using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProductReservation.Application.Common.Interfaces;
using ProductReservation.Infrastructure.Persistence;
using ProductReservation.Infrastructure.Services;
using ProductReservation.Infrastructure.Workers;
using Quartz;

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
        services.AddScoped<ExpiredReservationProcessor>();
        services.AddSingleton<IProductLock, ProductLock>();
        services.AddQuartz(quartz =>
        {
            quartz.ConfigureScheduler(options =>
                options.ShutdownJobInterruption = ShutdownJobInterruption.WhenWaitingForJobs);

            var jobKey = new JobKey(nameof(ReservationExpirationJob));
            quartz.AddJob<ReservationExpirationJob>(options => options.WithIdentity(jobKey));
            quartz.AddTrigger(options => options
                .ForJob(jobKey)
                .WithIdentity($"{jobKey.Name}-trigger")
                .StartNow()
                .WithSimpleSchedule(schedule => schedule
                    .WithInterval(TimeSpan.FromHours(1))
                    .RepeatForever())
                .WithRetryPolicy(RetryPolicy.Explicit(
                    TimeSpan.FromMinutes(1),
                    TimeSpan.FromMinutes(5),
                    TimeSpan.FromMinutes(15))));
        });
        services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

        return services;
    }
}
