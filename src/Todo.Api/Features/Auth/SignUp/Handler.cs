using BuildingBlocks.Application.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Todo.Api.Abstractions.Persistence;
using Todo.Api.Abstractions.Security;
using Todo.Api.Domain.Users;

namespace Todo.Api.Features.Auth.SignUp;

public sealed class Handler(IApplicationDbContext dbContext, IPasswordHasher passwordHasher)
    : IRequestHandler<Command, Result<Response>>
{
    public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();
        var userName = request.UserName.Trim();

        if (await dbContext.Users.AnyAsync(x => x.Email == email, cancellationToken))
        {
            return Result<Response>.Failure(Auth.AuthErrors.DuplicateEmail);
        }

        if (await dbContext.Users.AnyAsync(x => x.UserName == userName, cancellationToken))
        {
            return Result<Response>.Failure(Auth.AuthErrors.DuplicateUserName);
        }

        var passwordHash = passwordHasher.Hash(request.Password);
        var user = User.Register(email, userName, passwordHash, DateTime.UtcNow);

        dbContext.Users.Add(user);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } postgresException)
        {
            switch (postgresException.ConstraintName)
            {
                case "IX_users_Email":
                    return Result<Response>.Failure(Auth.AuthErrors.DuplicateEmail);
                case "IX_users_UserName":
                    return Result<Response>.Failure(Auth.AuthErrors.DuplicateUserName);
                default:
                    throw;
            }
        }

        return Result<Response>.Success(new Response(user.Id, user.Email, user.UserName));
    }
}
