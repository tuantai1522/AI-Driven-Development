namespace Todo.Api.Abstractions.Security;

public interface IJwtTokenGenerator
{
    string Generate(Guid userId, string email, string userName);
}
