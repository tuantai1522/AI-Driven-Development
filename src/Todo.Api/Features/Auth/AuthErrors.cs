using BuildingBlocks.Application.Results;

namespace Todo.Api.Features.Auth;

public static class AuthErrors
{
    public static readonly Error DuplicateEmail =
        new("auth.duplicate_email", "The email address is already in use.", ErrorType.Conflict);

    public static readonly Error DuplicateUserName =
        new("auth.duplicate_user_name", "The user name is already in use.", ErrorType.Conflict);
}
