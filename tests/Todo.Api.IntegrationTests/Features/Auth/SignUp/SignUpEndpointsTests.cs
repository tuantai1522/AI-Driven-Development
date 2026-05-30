using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Todo.Api.IntegrationTests.Fixtures;
using Todo.Api.Persistence;

namespace Todo.Api.IntegrationTests.Features.Auth.SignUp;

public sealed class SignUpEndpointsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Post_SignUp_Should_Return_Created_And_Persist_User()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/sign-up", new
        {
            email = "user@example.com",
            password = "secret123",
            userName = "sampleUser"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<SignUpResponse>();
        body.Should().NotBeNull();
        body!.Email.Should().Be("user@example.com");
        body.UserName.Should().Be("sampleUser");

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await dbContext.Users.SingleAsync(x => x.Id == body.Id);

        user.PasswordHash.Should().NotBe("secret123");
    }

    [Fact]
    public async Task Post_SignUp_Should_Return_Conflict_For_Duplicate_Email()
    {
        var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/v1/auth/sign-up", new
        {
            email = "duplicate@example.com",
            password = "secret123",
            userName = "firstUser"
        });

        var response = await client.PostAsJsonAsync("/api/v1/auth/sign-up", new
        {
            email = "duplicate@example.com",
            password = "secret123",
            userName = "secondUser"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be((int)HttpStatusCode.Conflict);
        problem.Title.Should().Be("Conflict");
        problem.Type.Should().Be("auth.duplicate_email");
        problem.Detail.Should().Be("The email address is already in use.");
    }

    [Fact]
    public async Task Post_SignUp_Should_Return_Conflict_For_Duplicate_UserName()
    {
        var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/v1/auth/sign-up", new
        {
            email = "first@example.com",
            password = "secret123",
            userName = "duplicateUser"
        });

        var response = await client.PostAsJsonAsync("/api/v1/auth/sign-up", new
        {
            email = "second@example.com",
            password = "secret123",
            userName = "duplicateUser"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be((int)HttpStatusCode.Conflict);
        problem.Title.Should().Be("Conflict");
        problem.Type.Should().Be("auth.duplicate_user_name");
        problem.Detail.Should().Be("The user name is already in use.");
    }

    [Fact]
    public async Task Post_SignUp_Should_Not_Return_PasswordHash()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/sign-up", new
        {
            email = "hidden@example.com",
            password = "secret123",
            userName = "hiddenUser"
        });

        var json = await response.Content.ReadAsStringAsync();

        json.Should().NotContainEquivalentOf("password");
        json.Should().NotContainEquivalentOf("passwordHash");
    }

    private sealed record SignUpResponse(Guid Id, string Email, string UserName);
}
