using BuildingBlocks.Application.Results;
using MediatR;

namespace Todo.Api.Features.Auth.SignUp;

public sealed record Command(string Email, string Password, string UserName) : IRequest<Result<Response>>;
