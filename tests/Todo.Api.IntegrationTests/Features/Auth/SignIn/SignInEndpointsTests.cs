using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Todo.Api.IntegrationTests.Fixtures;
using Todo.Api.Persistence;
using Todo.Api.Security;

namespace Todo.Api.IntegrationTests.Features.Auth.SignIn;

public sealed partial class SignInEndpointsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Post_SignIn_Should_Return_Ok_With_AccessToken_For_Valid_Credentials()
    {
        await SeedUserAsync("user@example.com", "sampleUser", "secret123");
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/sign-in", new
        {
            email = "user@example.com",
            password = "secret123"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<SignInResponse>();
        body.Should().NotBeNull();
        body.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Post_SignIn_Should_Return_Token_With_Sub_Email_And_UniqueName_Claims()
    {
        await SeedUserAsync("claims@example.com", "claimsUser", "secret123");
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/sign-in", new
        {
            email = "claims@example.com",
            password = "secret123"
        });

        var body = await response.Content.ReadFromJsonAsync<SignInResponse>();
        var token = new JwtSecurityTokenHandler().ReadJwtToken(body!.AccessToken);

        token.Claims.Should().Contain(x => x.Type == JwtRegisteredClaimNames.Sub);
        token.Claims.Should().Contain(x => x.Type == JwtRegisteredClaimNames.Email && x.Value == "claims@example.com");
        token.Claims.Should().Contain(x => x.Type == JwtRegisteredClaimNames.UniqueName && x.Value == "claimsUser");
    }

    [Fact]
    public async Task Post_SignIn_Should_Return_Unauthorized_For_Unknown_Email()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/sign-in", new
        {
            email = "missing@example.com",
            password = "secret123"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem.Title.Should().Be("Unauthorized");
        problem.Type.Should().Be("auth.invalid_credentials");
        problem.Detail.Should().Be("The email or password is incorrect.");
    }

    [Fact]
    public async Task Post_SignIn_Should_Return_Unauthorized_For_Wrong_Password()
    {
        await SeedUserAsync("wrongpass@example.com", "wrongPassUser", "secret123");
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/sign-in", new
        {
            email = "wrongpass@example.com",
            password = "not-the-password"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem.Type.Should().Be("auth.invalid_credentials");
        problem.Detail.Should().Be("The email or password is incorrect.");
    }

    private async Task SeedUserAsync(string email, string userName, string password)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hasher = new PasswordHasher();

        dbContext.Users.Add(Todo.Api.Domain.Users.User.Register(
            email,
            userName,
            hasher.Hash(password),
            DateTime.UtcNow));

        await dbContext.SaveChangesAsync();
    }

    private sealed record SignInResponse(string AccessToken);
}
