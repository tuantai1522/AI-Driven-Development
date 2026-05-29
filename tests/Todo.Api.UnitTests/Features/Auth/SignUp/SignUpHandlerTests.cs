using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Todo.Api.Abstractions.Security;
using Todo.Api.Domain.Users;
using Todo.Api.Features.Auth;
using Todo.Api.Features.Auth.SignUp;
using Todo.Api.Persistence;

namespace Todo.Api.UnitTests.Features.Auth.SignUp;

public sealed class SignUpHandlerTests
{
    [Fact]
    public async Task Handle_Should_Create_User_Successfully()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new ApplicationDbContext(options);
        var passwordHasher = Substitute.For<IPasswordHasher>();
        passwordHasher.Hash("secret123").Returns("HASHED");

        var handler = new Handler(dbContext, passwordHasher);

        var result = await handler.Handle(new Command("user@example.com", "secret123", "sampleUser"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        dbContext.Users.Should().ContainSingle(x =>
            x.Email == "user@example.com" &&
            x.UserName == "sampleUser" &&
            x.PasswordHash == "HASHED");
    }

    [Fact]
    public async Task Handle_Should_Return_Conflict_When_Email_Already_Exists()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new ApplicationDbContext(options);
        dbContext.Users.Add(User.Register("user@example.com", "existingUser", "HASHED", DateTime.UtcNow));
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var passwordHasher = Substitute.For<IPasswordHasher>();
        var handler = new Handler(dbContext, passwordHasher);

        var result = await handler.Handle(new Command("user@example.com", "secret123", "sampleUser"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AuthErrors.DuplicateEmail);
    }

    [Fact]
    public async Task Handle_Should_Return_Conflict_When_UserName_Already_Exists()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new ApplicationDbContext(options);
        dbContext.Users.Add(User.Register("other@example.com", "sampleUser", "HASHED", DateTime.UtcNow));
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var passwordHasher = Substitute.For<IPasswordHasher>();
        var handler = new Handler(dbContext, passwordHasher);

        var result = await handler.Handle(new Command("user@example.com", "secret123", "sampleUser"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AuthErrors.DuplicateUserName);
    }

    [Fact]
    public async Task Handle_Should_Use_Password_Hasher_Before_Persisting()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new ApplicationDbContext(options);
        var passwordHasher = Substitute.For<IPasswordHasher>();
        passwordHasher.Hash("secret123").Returns("HASHED");

        var handler = new Handler(dbContext, passwordHasher);

        await handler.Handle(new Command("user@example.com", "secret123", "sampleUser"), CancellationToken.None);

        passwordHasher.Received(1).Hash("secret123");
        dbContext.Users.Should().ContainSingle(x => x.PasswordHash == "HASHED");
    }
}
