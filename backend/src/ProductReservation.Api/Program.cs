using ProductReservation.Api;
using ProductReservation.Api.Endpoints;
using ProductReservation.Application;
using ProductReservation.Infrastructure;
using ProductReservation.Infrastructure.Persistence;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(configuration: builder.Configuration);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();

app.MapProductsEndpoints();
app.MapReservationsEndpoints();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => options.WithTitle("Product Reservation API"));
}

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
    await SeedData.InitializeAsync(
        dbContext,
        scope.ServiceProvider.GetRequiredService<TimeProvider>());
}

app.Run();

// Enables future WebApplicationFactory-based API tests.
public partial class Program;
