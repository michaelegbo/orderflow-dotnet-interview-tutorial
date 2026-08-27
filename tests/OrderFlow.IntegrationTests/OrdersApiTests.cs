using System.Net;
using System.Net.Http.Json;

namespace OrderFlow.IntegrationTests;

public sealed class OrdersApiTests(TestWebApplicationFactory factory)
    : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Create_requires_authentication()
    {
        var response = await _client.PostAsJsonAsync("/api/orders", ValidOrder());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_requires_the_manager_role()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/orders")
        {
            Content = JsonContent.Create(ValidOrder())
        };
        request.Headers.Add("X-Test-User", "ada");

        var response = await _client.SendAsync(request, CancellationToken.None);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Manager_can_create_then_read_an_order()
    {
        using var create = new HttpRequestMessage(HttpMethod.Post, "/api/orders")
        {
            Content = JsonContent.Create(ValidOrder())
        };
        create.Headers.Add("X-Test-User", "ada");
        create.Headers.Add("X-Test-Role", "OrderManager");

        var response = await _client.SendAsync(create, CancellationToken.None);
        var created = await response.Content.ReadFromJsonAsync<OrderResponse>(
            cancellationToken: CancellationToken.None);

        Assert.True(
            response.StatusCode == HttpStatusCode.Created,
            $"Expected 201 Created but received {(int)response.StatusCode}: " +
            await response.Content.ReadAsStringAsync(CancellationToken.None));
        Assert.NotNull(created);
        Assert.Equal(80m, created.Total);

        var fetched = await _client.GetAsync(
            $"/api/orders/{created.Id}",
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, fetched.StatusCode);
    }

    private static object ValidOrder() => new
    {
        customer = "Ada",
        lines = new[] { new { product = "Keyboard", quantity = 2, unitPrice = 40m } }
    };

    private sealed record OrderResponse(Guid Id, decimal Total);
}
