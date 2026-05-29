using BuildingBlocks.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Todo.Api.Features.Sample.Create;

public sealed class Endpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/sample", async (Command command, ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(command, cancellationToken);
                return result.IsSuccess
                    ? Results.CreatedAtRoute("GetSampleById", new { id = result.Value!.Id }, result.Value)
                    : Results.BadRequest(new ProblemDetails
                    {
                        Title = "Request failed",
                        Detail = result.Error!.Message,
                        Type = result.Error.Code,
                        Status = StatusCodes.Status400BadRequest
                    });
            })
            .WithName("CreateSample")
            .WithTags("Sample");
    }
}
