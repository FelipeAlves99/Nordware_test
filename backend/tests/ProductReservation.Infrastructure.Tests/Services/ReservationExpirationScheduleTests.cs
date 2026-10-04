using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProductReservation.Infrastructure;
using Quartz;
using Xunit;

namespace ProductReservation.Infrastructure.Tests.Services;

public sealed class ReservationExpirationScheduleTests
{
    [Fact]
    public async Task AddInfrastructure_RegistersConfiguredCronAndStartupTriggers()
    {
        const string cronExpression = "0 * * * * ?";
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Quartz:ReservationExpiration:CronExpression"] = cronExpression
            })
            .Build();
        services.AddInfrastructure(Guid.NewGuid().ToString(), configuration);

        await using var provider = services.BuildServiceProvider();
        var scheduler = await provider.GetRequiredService<ISchedulerFactory>().GetScheduler();

        var cronTrigger = await scheduler.GetTrigger(new TriggerKey("ReservationExpirationJob-cron-trigger"));
        var startupTrigger = await scheduler.GetTrigger(new TriggerKey("ReservationExpirationJob-startup-trigger"));

        Assert.NotNull(cronTrigger);
        Assert.Equal(cronExpression, Assert.IsAssignableFrom<ICronTrigger>(cronTrigger).CronExpressionString);
        Assert.Equal(
            RetryPolicy.Explicit(
                TimeSpan.FromMinutes(1),
                TimeSpan.FromMinutes(5),
                TimeSpan.FromMinutes(15)),
            cronTrigger.RetryPolicy);

        Assert.NotNull(startupTrigger);
        Assert.Equal(0, Assert.IsAssignableFrom<ISimpleTrigger>(startupTrigger).RepeatCount);

        await scheduler.Shutdown();
    }

    [Fact]
    public async Task AddInfrastructure_UsesOncePerMinuteCronWhenNoConfigurationIsProvided()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(Guid.NewGuid().ToString());

        await using var provider = services.BuildServiceProvider();
        var scheduler = await provider.GetRequiredService<ISchedulerFactory>().GetScheduler();
        var trigger = await scheduler.GetTrigger(new TriggerKey("ReservationExpirationJob-cron-trigger"));

        Assert.NotNull(trigger);
        Assert.Equal("0 * * * * ?", Assert.IsAssignableFrom<ICronTrigger>(trigger).CronExpressionString);

        await scheduler.Shutdown();
    }
}
