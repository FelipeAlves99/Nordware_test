using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ProductReservation.Application.Common.Interfaces;
using ProductReservation.Application.Reservations.ReserveProduct;
using ProductReservation.Infrastructure.Persistence;
using Xunit;

namespace ProductReservation.Api.Tests.Endpoints;

public sealed class ProductReservationApiTests
{
    private const string CustomerAId = "11111111-1111-1111-1111-111111111111";
    private const string CustomerBId = "22222222-2222-2222-2222-222222222222";
    private const string ProductAId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
    private const string ProductBId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
    private const string ProductCId = "cccccccc-cccc-cccc-cccc-cccccccccccc";

    [Fact]
    public async Task ApiDocumentation_ExposesOpenApiDocumentAndScalarUiInDevelopment()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var openApiResponse = await client.GetAsync("/openapi/v1.json");
        var scalarResponse = await client.GetAsync("/scalar");

        Assert.Equal(HttpStatusCode.OK, openApiResponse.StatusCode);
        using var openApiDocument = JsonDocument.Parse(await openApiResponse.Content.ReadAsStringAsync());
        Assert.True(openApiDocument.RootElement.GetProperty("paths").TryGetProperty("/products", out _));
        Assert.True(openApiDocument.RootElement.GetProperty("paths").TryGetProperty("/reservations/{reservationId}", out _));
        Assert.Equal(HttpStatusCode.OK, scalarResponse.StatusCode);
        Assert.Contains("Product Reservation API", await scalarResponse.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task GetProducts_ReturnsQuantitiesAndStatuses()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Customer-Id", CustomerAId);
        var reservedProductResponse = await client.PostAsJsonAsync(
            $"/products/{ProductAId}/reserve",
            new ReserveProductCommand(Guid.Empty, Guid.Empty, 1));
        var fullyReservedProductResponse = await client.PostAsJsonAsync(
            $"/products/{ProductBId}/reserve",
            new ReserveProductCommand(Guid.Empty, Guid.Empty, 1));
        Assert.Equal(HttpStatusCode.Created, reservedProductResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, fullyReservedProductResponse.StatusCode);

        using var productsBody = JsonDocument.Parse(await client.GetStringAsync("/products"));
        var products = productsBody.RootElement.EnumerateArray().ToArray();
        Assert.Collection(
            products,
            product =>
            {
                Assert.Equal("Produto A", product.GetProperty("name").GetString());
                Assert.Equal(10, product.GetProperty("totalQuantity").GetInt32());
                Assert.Equal(2, product.GetProperty("reservedQuantity").GetInt32());
                Assert.Equal(8, product.GetProperty("availableQuantity").GetInt32());
                Assert.Equal("Available", product.GetProperty("status").GetString());
            },
            product => Assert.Equal("Reserved", product.GetProperty("status").GetString()),
            product => Assert.Equal("Unavailable", product.GetProperty("status").GetString()));
    }

    [Fact]
    public async Task PostReserve_ReturnsCreatedReservationWithUtcDates()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Customer-Id", CustomerAId);

        var response = await client.PostAsJsonAsync(
            $"/products/{ProductAId}/reserve",
            new { quantity = 3 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = body.RootElement;
        Assert.Equal(CustomerAId, root.GetProperty("customerId").GetString());
        Assert.Equal(ProductAId, root.GetProperty("productId").GetString());
        Assert.Equal(3, root.GetProperty("quantity").GetInt32());
        Assert.Equal("Active", root.GetProperty("status").GetString());
        Assert.Equal(TimeSpan.Zero, DateTimeOffset.Parse(root.GetProperty("createdAtUtc").GetString()!).Offset);
        Assert.Equal(TimeSpan.Zero, DateTimeOffset.Parse(root.GetProperty("expiresAtUtc").GetString()!).Offset);
    }

    [Fact]
    public async Task PostReserve_ReportsMissingOrInvalidCustomerHeaderAsBadRequest()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var missingHeaderResponse = await client.PostAsJsonAsync(
            $"/products/{ProductAId}/reserve",
            new ReserveProductCommand(Guid.Empty, Guid.Empty, 1));
        Assert.Equal(HttpStatusCode.BadRequest, missingHeaderResponse.StatusCode);
        Assert.Equal("CustomerIdRequired", await ReadProblemCodeAsync(missingHeaderResponse));

        client.DefaultRequestHeaders.Add("X-Customer-Id", "not-a-guid");
        var invalidHeaderResponse = await client.PostAsJsonAsync(
            $"/products/{ProductAId}/reserve",
            new ReserveProductCommand(Guid.Empty, Guid.Empty, 1));
        Assert.Equal(HttpStatusCode.BadRequest, invalidHeaderResponse.StatusCode);
        Assert.Equal("InvalidCustomerId", await ReadProblemCodeAsync(invalidHeaderResponse));
    }

    [Fact]
    public async Task PostReserve_ReportsInvalidQuantityAsBadRequest()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Customer-Id", CustomerAId);

        var response = await client.PostAsJsonAsync(
            $"/products/{ProductAId}/reserve",
            new ReserveProductCommand(Guid.Empty, Guid.Empty, 0));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("ValidationError", await ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task PostReserve_ReportsMissingCustomerAndProductAsNotFound()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Customer-Id", "33333333-3333-3333-3333-333333333333");

        var missingCustomerResponse = await client.PostAsJsonAsync(
            $"/products/{ProductAId}/reserve",
            new ReserveProductCommand(Guid.Empty, Guid.Empty, 1));
        Assert.Equal(HttpStatusCode.NotFound, missingCustomerResponse.StatusCode);
        Assert.Equal("CustomerNotFound", await ReadProblemCodeAsync(missingCustomerResponse));

        client.DefaultRequestHeaders.Remove("X-Customer-Id");
        client.DefaultRequestHeaders.Add("X-Customer-Id", CustomerAId);
        var missingProductResponse = await client.PostAsJsonAsync(
            "/products/dddddddd-dddd-dddd-dddd-dddddddddddd/reserve",
            new ReserveProductCommand(Guid.Empty, Guid.Empty, 1));
        Assert.Equal(HttpStatusCode.NotFound, missingProductResponse.StatusCode);
        Assert.Equal("ProductNotFound", await ReadProblemCodeAsync(missingProductResponse));
    }

    [Fact]
    public async Task PostReserve_ReportsInsufficientStockAsConflict()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Customer-Id", CustomerAId);

        var response = await client.PostAsJsonAsync(
            $"/products/{ProductCId}/reserve",
            new ReserveProductCommand(Guid.Empty, Guid.Empty, 1));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("ProductUnavailable", await ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task DeleteReservation_ByIdIsIdempotentAndLeavesOtherReservationsUnchanged()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Customer-Id", CustomerAId);

        var firstReserveResponse = await client.PostAsJsonAsync(
            $"/products/{ProductAId}/reserve",
            new ReserveProductCommand(Guid.Empty, Guid.Empty, 2));
        var secondReserveResponse = await client.PostAsJsonAsync(
            $"/products/{ProductAId}/reserve",
            new ReserveProductCommand(Guid.Empty, Guid.Empty, 3));
        Assert.Equal(HttpStatusCode.Created, firstReserveResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, secondReserveResponse.StatusCode);

        using var firstReservationBody = JsonDocument.Parse(await firstReserveResponse.Content.ReadAsStringAsync());
        using var secondReservationBody = JsonDocument.Parse(await secondReserveResponse.Content.ReadAsStringAsync());
        var firstReservationId = firstReservationBody.RootElement.GetProperty("id").GetString();
        var secondReservationId = secondReservationBody.RootElement.GetProperty("id").GetString();

        var firstDelete = await client.DeleteAsync($"/reservations/{firstReservationId}");
        var repeatedDelete = await client.DeleteAsync($"/reservations/{firstReservationId}");
        Assert.Equal(HttpStatusCode.NoContent, firstDelete.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, repeatedDelete.StatusCode);

        using var reservationsBody = JsonDocument.Parse(
            await client.GetStringAsync($"/customer/{CustomerAId}/reservations"));
        var reservations = reservationsBody.RootElement.EnumerateArray().ToArray();
        var cancelledReservation = Assert.Single(reservations,
            item => item.GetProperty("id").GetString() == firstReservationId);
        var stillActiveReservation = Assert.Single(reservations,
            item => item.GetProperty("id").GetString() == secondReservationId);
        Assert.Equal("Cancelled", cancelledReservation.GetProperty("status").GetString());
        Assert.NotEqual(JsonValueKind.Null, cancelledReservation.GetProperty("cancelledAtUtc").ValueKind);
        Assert.Equal("Active", stillActiveReservation.GetProperty("status").GetString());
    }

    [Fact]
    public async Task DeleteReservation_RequiresCustomerHeaderAndMatchingOwner()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Customer-Id", CustomerAId);

        var reserveResponse = await client.PostAsJsonAsync(
            $"/products/{ProductAId}/reserve",
            new { quantity = 1 });
        Assert.Equal(HttpStatusCode.Created, reserveResponse.StatusCode);
        using var reservationBody = JsonDocument.Parse(await reserveResponse.Content.ReadAsStringAsync());
        var reservationId = reservationBody.RootElement.GetProperty("id").GetString();

        client.DefaultRequestHeaders.Remove("X-Customer-Id");
        var missingHeaderResponse = await client.DeleteAsync($"/reservations/{reservationId}");
        Assert.Equal(HttpStatusCode.BadRequest, missingHeaderResponse.StatusCode);
        Assert.Equal("CustomerIdRequired", await ReadProblemCodeAsync(missingHeaderResponse));

        client.DefaultRequestHeaders.Add("X-Customer-Id", CustomerBId);
        var wrongCustomerResponse = await client.DeleteAsync($"/reservations/{reservationId}");
        Assert.Equal(HttpStatusCode.BadRequest, wrongCustomerResponse.StatusCode);
        Assert.Equal("InvalidRequest", await ReadProblemCodeAsync(wrongCustomerResponse));

        client.DefaultRequestHeaders.Remove("X-Customer-Id");
        client.DefaultRequestHeaders.Add("X-Customer-Id", CustomerAId);
        var ownerResponse = await client.DeleteAsync($"/reservations/{reservationId}");
        Assert.Equal(HttpStatusCode.NoContent, ownerResponse.StatusCode);
    }

    [Fact]
    public async Task ConcurrentReservationsForSameProduct_DoNotExceedAvailableStock()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var firstRequest = CreateReserveRequest(CustomerAId, 6);
        var secondRequest = CreateReserveRequest(CustomerBId, 6);
        var responses = await Task.WhenAll(
            client.SendAsync(firstRequest),
            client.SendAsync(secondRequest));

        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Created);
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict);

        using var productsBody = JsonDocument.Parse(await client.GetStringAsync("/products"));
        var product = Assert.Single(productsBody.RootElement.EnumerateArray(),
            item => item.GetProperty("id").GetString() == ProductAId);
        Assert.Equal(7, product.GetProperty("reservedQuantity").GetInt32());
    }

    [Fact]
    public async Task SyntheticConcurrentReservations_NeverExceedProductStock()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        const int requestCount = 100;
        var requests = Enumerable.Range(0, requestCount)
            .Select(_ => CreateReserveRequest(CustomerAId, quantity: 1))
            .ToArray();
        HttpResponseMessage[]? responses = null;

        try
        {
            responses = await Task.WhenAll(requests.Select(client.SendAsync));

            Assert.Equal(9, responses.Count(response => response.StatusCode == HttpStatusCode.Created));
            Assert.Equal(91, responses.Count(response => response.StatusCode == HttpStatusCode.Conflict));

            using var productsBody = JsonDocument.Parse(await client.GetStringAsync("/products"));
            var product = Assert.Single(productsBody.RootElement.EnumerateArray(),
                item => item.GetProperty("id").GetString() == ProductAId);
            Assert.Equal(10, product.GetProperty("reservedQuantity").GetInt32());
            Assert.Equal(0, product.GetProperty("availableQuantity").GetInt32());
            Assert.Equal("Reserved", product.GetProperty("status").GetString());
        }
        finally
        {
            foreach (var request in requests)
            {
                request.Dispose();
            }

            if (responses is not null)
            {
                foreach (var response in responses)
                {
                    response.Dispose();
                }
            }
        }
    }

    private static HttpRequestMessage CreateReserveRequest(string customerId, int quantity)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/products/{ProductAId}/reserve")
        {
            Content = JsonContent.Create(new { quantity })
        };
        request.Headers.Add("X-Customer-Id", customerId);
        return request;
    }

    private static async Task<string?> ReadProblemCodeAsync(HttpResponseMessage response)
    {
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("code").GetString();
    }

    private sealed class ApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = Guid.NewGuid().ToString();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<AppDbContext>();
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<IAppDbContext>();
                services.RemoveAll<TimeProvider>();
                services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(_databaseName));
                services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());
                services.AddSingleton<TimeProvider>(new FixedTimeProvider(DateTimeOffset.UtcNow));
            });
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset nowUtc) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => nowUtc;
    }
}
