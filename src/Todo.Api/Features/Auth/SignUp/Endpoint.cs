using BuildingBlocks.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Todo.Api.Features.Auth.SignUp;

public sealed class Endpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/sign-up", async (Command command, ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(command, cancellationToken);

                if (result.IsSuccess)
                {
                    return Results.Created("/api/v1/auth/sign-up", result.Value);
                }

                var statusCode = result.Error == Auth.AuthErrors.DuplicateEmail || result.Error == Auth.AuthErrors.DuplicateUserName
                    ? StatusCodes.Status409Conflict
                    : StatusCodes.Status400BadRequest;

                return Results.Problem(new ProblemDetails
                {
                    Status = statusCode,
                    Title = statusCode == StatusCodes.Status409Conflict ? "Duplicate user field." : "Request failed",
                    Detail = result.Error!.Message,
                    Type = result.Error.Code
                });
            })
            .WithName("AuthSignUp")
            .WithTags("Auth");
    }
}
