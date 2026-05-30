using BuildingBlocks.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Routing;
using Todo.Api.ExceptionHandling;

namespace Todo.Api.Features.Auth.SignIn;

public sealed class Endpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/sign-in", async (Command command, ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(command, cancellationToken);

                if (result.IsSuccess)
                {
                    return Results.Ok(result.Value);
                }

                var problemDetails = ErrorProblemDetailsMapper.Map(result.Error!);

                return Results.Problem(
                    title: problemDetails.Title,
                    type: problemDetails.Type,
                    detail: problemDetails.Detail,
                    statusCode: problemDetails.Status);
            })
            .WithName("AuthSignIn")
            .WithTags("Auth");
    }
}
