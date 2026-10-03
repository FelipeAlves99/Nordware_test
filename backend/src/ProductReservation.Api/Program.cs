using Microsoft.EntityFrameworkCore;
using ProductReservation.Application;
using ProductReservation.Infrastructure;
using ProductReservation.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApplication();
builder.Services.AddInfrastructure();

var app = builder.Build();

// Endpoint groups will be registered here after the use cases are implemented.
await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

app.Run();

// Enables future WebApplicationFactory-based API tests.
public partial class Program;
