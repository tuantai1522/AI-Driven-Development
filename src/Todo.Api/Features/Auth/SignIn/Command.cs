using BuildingBlocks.Application.Results;
using MediatR;

namespace Todo.Api.Features.Auth.SignIn;

public sealed record Command(string Email, string Password) : IRequest<Result<Response>>;
