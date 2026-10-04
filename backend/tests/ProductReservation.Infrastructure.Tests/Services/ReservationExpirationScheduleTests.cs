using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProductReservation.Infrastructure;
using Quartz;
using Xunit;

namespace ProductReservation.Infrastructure.Tests.Services;

public sealed class ReservationExpirationScheduleTests
{
    [Fact]
    public async Task AddInfrastructure_RegistersHourlyExpirationTriggerWithBoundedRetries()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(Guid.NewGuid().ToString());

        await using var provider = services.BuildServiceProvider();
        var scheduler = await provider.GetRequiredService<ISchedulerFactory>().GetScheduler();

        var trigger = await scheduler.GetTrigger(new TriggerKey("ReservationExpirationJob-trigger"));

        Assert.NotNull(trigger);
        Assert.Equal(TimeSpan.FromHours(1), Assert.IsAssignableFrom<ISimpleTrigger>(trigger).RepeatInterval);
        Assert.Equal(
            RetryPolicy.Explicit(
                TimeSpan.FromMinutes(1),
                TimeSpan.FromMinutes(5),
                TimeSpan.FromMinutes(15)),
            trigger.RetryPolicy);

        await scheduler.Shutdown();
    }
}
