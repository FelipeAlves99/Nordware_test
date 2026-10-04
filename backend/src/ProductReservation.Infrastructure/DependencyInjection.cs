using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        string databaseName = DefaultDatabaseName,
        IConfiguration? configuration = null)
    {
        const string defaultExpirationCronExpression = "0 * * * * ?";
        var expirationCronExpression = configuration?
            .GetValue<string>("Quartz:ReservationExpiration:CronExpression")
            ?? defaultExpirationCronExpression;

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
                .WithIdentity($"{jobKey.Name}-startup-trigger")
                .StartNow()
                .WithSimpleSchedule(schedule => schedule.WithRepeatCount(0))
                .WithRetryPolicy(RetryPolicy.Explicit(
                    TimeSpan.FromMinutes(1),
                    TimeSpan.FromMinutes(5),
                    TimeSpan.FromMinutes(15))));
            quartz.AddTrigger(options => options
                .ForJob(jobKey)
                .WithIdentity($"{jobKey.Name}-cron-trigger")
                .WithCronSchedule(expirationCronExpression)
                .WithRetryPolicy(RetryPolicy.Explicit(
                    TimeSpan.FromMinutes(1),
                    TimeSpan.FromMinutes(5),
                    TimeSpan.FromMinutes(15))));
        });
        services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

        return services;
    }
}
