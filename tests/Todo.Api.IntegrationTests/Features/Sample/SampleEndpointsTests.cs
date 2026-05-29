using System.Net;
using System.Net.Http.Json;
using Todo.Api.IntegrationTests.Fixtures;
using FluentAssertions;

namespace Todo.Api.IntegrationTests.Features.Sample;

public sealed class SampleEndpointsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Post_Then_Get_Should_Roundtrip_Sample_Item()
    {
        var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/v1/sample", new { name = "roundtrip" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<CreateSampleResponse>();
        var getResponse = await client.GetAsync($"/api/v1/sample/{created!.Id}");

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private sealed record CreateSampleResponse(Guid Id, string Name, DateTime CreatedUtc);
}
