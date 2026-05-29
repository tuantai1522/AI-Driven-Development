namespace Todo.Api.Domain.Users;

public sealed class User
{
    private User()
    {
    }

    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public string Email { get; private set; } = string.Empty;
    public string UserName { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }

    public static User Register(string email, string userName, string passwordHash, DateTime createdAtUtc)
    {
        return new User
        {
            Email = email.Trim(),
            UserName = userName.Trim(),
            PasswordHash = passwordHash,
            CreatedAtUtc = createdAtUtc
        };
    }
}
