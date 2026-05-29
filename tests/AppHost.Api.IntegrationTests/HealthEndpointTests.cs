using System.Net;
using AppHost.Api.IntegrationTests.Fixtures;
using FluentAssertions;

namespace AppHost.Api.IntegrationTests;

public sealed class HealthEndpointTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Get_Health_Should_Return_Ok()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
