using Microsoft.EntityFrameworkCore;
using ProductReservation.Api;
using ProductReservation.Api.Endpoints;
using ProductReservation.Application;
using ProductReservation.Infrastructure;
using ProductReservation.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApplication();
builder.Services.AddInfrastructure();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler();

app.MapProductsEndpoints();
app.MapReservationsEndpoints();
await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

app.Run();

// Enables future WebApplicationFactory-based API tests.
public partial class Program;
