using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Todo.Api.Abstractions.Security;
using Todo.Api.Domain.Users;
using Todo.Api.Features.Auth;
using Todo.Api.Features.Auth.SignIn;
using Todo.Api.Persistence;

namespace Todo.Api.UnitTests.Features.Auth.SignIn;

public sealed class SignInHandlerTests
{
    [Fact]
    public async Task Handle_Should_Return_AccessToken_For_Valid_Credentials()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new ApplicationDbContext(options);
        dbContext.Users.Add(User.Register("user@example.com", "sampleUser", "HASHED", DateTime.UtcNow));
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var passwordHasher = Substitute.For<IPasswordHasher>();
        passwordHasher.Verify("secret123", "HASHED").Returns(true);

        var jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
        jwtTokenGenerator.Generate(Arg.Any<Guid>(), "user@example.com", "sampleUser")
            .Returns("jwt-token");

        var handler = new Handler(dbContext, passwordHasher, jwtTokenGenerator);

        var result = await handler.Handle(new Command("user@example.com", "secret123"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(new Response("jwt-token"));
    }

    [Fact]
    public async Task Handle_Should_Return_Unauthorized_When_Email_Does_Not_Exist()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new ApplicationDbContext(options);
        var passwordHasher = Substitute.For<IPasswordHasher>();
        var jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
        var handler = new Handler(dbContext, passwordHasher, jwtTokenGenerator);

        var result = await handler.Handle(new Command("missing@example.com", "secret123"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AuthErrors.InvalidCredentials);
        jwtTokenGenerator.DidNotReceiveWithAnyArgs().Generate(default, default!, default!);
    }

    [Fact]
    public async Task Handle_Should_Return_Unauthorized_When_Password_Is_Incorrect()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new ApplicationDbContext(options);
        dbContext.Users.Add(User.Register("user@example.com", "sampleUser", "HASHED", DateTime.UtcNow));
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var passwordHasher = Substitute.For<IPasswordHasher>();
        passwordHasher.Verify("wrong-password", "HASHED").Returns(false);

        var jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
        var handler = new Handler(dbContext, passwordHasher, jwtTokenGenerator);

        var result = await handler.Handle(new Command("user@example.com", "wrong-password"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AuthErrors.InvalidCredentials);
        jwtTokenGenerator.DidNotReceiveWithAnyArgs().Generate(default, default!, default!);
    }
}
