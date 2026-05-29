using Microsoft.AspNetCore.Routing;

namespace BuildingBlocks.Abstractions;

public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}
