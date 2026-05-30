using BuildingBlocks.Application.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Todo.Api.Abstractions.Persistence;
using Todo.Api.Abstractions.Security;

namespace Todo.Api.Features.Auth.SignIn;

public sealed class Handler(
    IApplicationDbContext dbContext,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator) : IRequestHandler<Command, Result<Response>>
{
    public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();

        var user = await dbContext.Users.SingleOrDefaultAsync(x => x.Email == email, cancellationToken);
        if (user is null)
        {
            return Result<Response>.Failure(Auth.AuthErrors.InvalidCredentials);
        }

        var isPasswordValid = passwordHasher.Verify(request.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            return Result<Response>.Failure(Auth.AuthErrors.InvalidCredentials);
        }

        var accessToken = jwtTokenGenerator.Generate(user.Id, user.Email, user.UserName);

        return Result<Response>.Success(new Response(accessToken));
    }
}
