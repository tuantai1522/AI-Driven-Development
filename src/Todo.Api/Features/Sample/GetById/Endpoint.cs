using BuildingBlocks.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Todo.Api.Features.Sample.GetById;

public sealed class Endpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/sample/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new Query(id), cancellationToken);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.NotFound(new ProblemDetails
                    {
                        Title = "Resource not found",
                        Detail = result.Error!.Message,
                        Type = result.Error.Code,
                        Status = StatusCodes.Status404NotFound
                    });
            })
            .WithName("GetSampleById")
            .WithTags("Sample");
    }
}
