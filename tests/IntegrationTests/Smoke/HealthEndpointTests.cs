using FluentAssertions;
using Xunit;

namespace IntegrationTests.Smoke;

public class HealthEndpointTests : IClassFixture<EmmWebApplicationFactory>
{
    private readonly EmmWebApplicationFactory _factory;
    public HealthEndpointTests(EmmWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Health_endpoint_returns_OK()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync(new Uri("/health", UriKind.Relative));
        response.IsSuccessStatusCode.Should().BeTrue();
    }
}
