# User Sign-In JWT Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add `POST /api/v1/auth/sign-in` that validates credentials for an existing user and returns a JWT bearer access token.

**Architecture:** Keep the feature inside the existing vertical-slice structure: `Endpoint -> Command -> Validator -> Handler`. Password verification stays behind `IPasswordHasher`, JWT creation stays behind a new `IJwtTokenGenerator`, and JWT bearer authentication is registered centrally through the existing service and pipeline extension points so later `[Authorize]` work can reuse it without revisiting the auth foundation.

**Tech Stack:** .NET 10, ASP.NET Core Minimal API, MediatR, FluentValidation, EF Core 10, Npgsql, PBKDF2 password hashing, JWT bearer authentication, xUnit, FluentAssertions, NSubstitute, Microsoft.AspNetCore.Mvc.Testing, Testcontainers.PostgreSql

---

## File Structure

- `src/Todo.Api/Abstractions/Security/IPasswordHasher.cs`
  Responsibility: hashing and verification contract used by auth handlers.
- `src/Todo.Api/Abstractions/Security/IJwtTokenGenerator.cs`
  Responsibility: token issuance contract for feature code.
- `src/Todo.Api/Security/PasswordHasher.cs`
  Responsibility: PBKDF2 hashing plus verification of the existing stored format.
- `src/Todo.Api/Security/Jwt/JwtOptions.cs`
  Responsibility: strongly typed JWT configuration with a 60-minute default lifetime.
- `src/Todo.Api/Security/Jwt/JwtTokenGenerator.cs`
  Responsibility: create signed access tokens with `sub`, `email`, and `unique_name`.
- `src/Todo.Api/Features/Auth/AuthErrors.cs`
  Responsibility: auth-specific errors, including generic invalid-credentials failure.
- `src/Todo.Api/Features/Auth/SignIn/Command.cs`
- `src/Todo.Api/Features/Auth/SignIn/Response.cs`
- `src/Todo.Api/Features/Auth/SignIn/Validator.cs`
- `src/Todo.Api/Features/Auth/SignIn/Handler.cs`
- `src/Todo.Api/Features/Auth/SignIn/Endpoint.cs`
  Responsibility: sign-in vertical slice.
- `src/Todo.Api/Extensions/ServiceCollectionExtensions.cs`
  Responsibility: register JWT options, token generator, auth services, and security services.
- `src/Todo.Api/Extensions/ApplicationExtensions.cs`
  Responsibility: add authentication before authorization in the HTTP pipeline.
- `src/Todo.Api/appsettings.json`
- `src/Todo.Api/appsettings.Development.json`
  Responsibility: provide local JWT settings and keep the lifetime default visible.
- `src/Todo.Api/Todo.Api.csproj`
  Responsibility: include JWT bearer package support if it is not already transitively available.
- `tests/Todo.Api.UnitTests/Security/PasswordHasherTests.cs`
  Responsibility: verify correct and incorrect password verification behavior.
- `tests/Todo.Api.UnitTests/Features/Auth/SignIn/SignInValidatorTests.cs`
- `tests/Todo.Api.UnitTests/Features/Auth/SignIn/SignInHandlerTests.cs`
  Responsibility: sign-in validator and handler behavior.
- `tests/Todo.Api.IntegrationTests/Features/Auth/SignIn/SignInEndpointsTests.cs`
  Responsibility: end-to-end sign-in contract and JWT claims verification.

## Task 1: Add JWT Contracts and Generator

**Files:**

- Modify: `src/Todo.Api/Todo.Api.csproj`
- Create: `src/Todo.Api/Abstractions/Security/IJwtTokenGenerator.cs`
- Create: `src/Todo.Api/Security/Jwt/JwtOptions.cs`
- Create: `src/Todo.Api/Security/Jwt/JwtTokenGenerator.cs`
- Create: `tests/Todo.Api.UnitTests/Security/Jwt/JwtTokenGeneratorTests.cs`

- [ ] **Step 1: Write the failing JWT generator test**

```csharp
// tests/Todo.Api.UnitTests/Security/Jwt/JwtTokenGeneratorTests.cs
using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Todo.Api.Security.Jwt;

namespace Todo.Api.UnitTests.Security.Jwt;

public sealed class JwtTokenGeneratorTests
{
    [Fact]
    public void Generate_Should_Create_Token_With_Expected_Claims_And_Expiry()
    {
        var options = Options.Create(new JwtOptions
        {
            Issuer = "Todo.Api",
            Audience = "Todo.Api.Client",
            SigningKey = "development-signing-key-change-me-1234567890",
            AccessTokenLifetimeMinutes = 60
        });
        var generator = new JwtTokenGenerator(options);

        var accessToken = generator.Generate(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "user@example.com",
            "sampleUser");

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(accessToken);

        token.Issuer.Should().Be("Todo.Api");
        token.Audiences.Should().Contain("Todo.Api.Client");
        token.Claims.Should().Contain(x => x.Type == JwtRegisteredClaimNames.Sub && x.Value == "11111111-1111-1111-1111-111111111111");
        token.Claims.Should().Contain(x => x.Type == JwtRegisteredClaimNames.Email && x.Value == "user@example.com");
        token.Claims.Should().Contain(x => x.Type == JwtRegisteredClaimNames.UniqueName && x.Value == "sampleUser");
        token.ValidTo.Should().BeAfter(DateTime.UtcNow.AddMinutes(59));
    }
}
```

- [ ] **Step 2: Run the test to confirm it fails**

Run: `dotnet test tests/Todo.Api.UnitTests/Todo.Api.UnitTests.csproj --filter JwtTokenGeneratorTests -v minimal`
Expected: FAIL because the JWT generator types do not exist yet.

- [ ] **Step 3: Add the JWT abstraction, options, package reference, and implementation**

```xml
<!-- src/Todo.Api/Todo.Api.csproj -->
<Project Sdk="Microsoft.NET.Sdk.Web">

  <ItemGroup>
    <ProjectReference Include="..\BuildingBlocks\BuildingBlocks.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="FluentValidation" Version="12.1.1" />
    <PackageReference Include="FluentValidation.DependencyInjectionExtensions" Version="12.1.1" />
    <PackageReference Include="MediatR" Version="14.1.0" />
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.8" />
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.0.8" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.4">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.4" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.2" />
    <PackageReference Include="Scalar.AspNetCore" Version="2.14.14" />
    <PackageReference Include="Serilog.AspNetCore" Version="10.0.0" />
  </ItemGroup>

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

</Project>
```

```csharp
// src/Todo.Api/Abstractions/Security/IJwtTokenGenerator.cs
namespace Todo.Api.Abstractions.Security;

public interface IJwtTokenGenerator
{
    string Generate(Guid userId, string email, string userName);
}
```

```csharp
// src/Todo.Api/Security/Jwt/JwtOptions.cs
namespace Todo.Api.Security.Jwt;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string SigningKey { get; init; } = string.Empty;
    public int AccessTokenLifetimeMinutes { get; init; } = 60;
}
```

```csharp
// src/Todo.Api/Security/Jwt/JwtTokenGenerator.cs
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Todo.Api.Abstractions.Security;
namespace Todo.Api.Security.Jwt;

public sealed class JwtTokenGenerator(IOptions<JwtOptions> options) : IJwtTokenGenerator
{
    private readonly JwtOptions _options = options.Value;

    public string Generate(Guid userId, string email, string userName)
    {
        var now = DateTime.UtcNow;
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(JwtRegisteredClaimNames.UniqueName, userName)
            ]),
            Expires = now.AddMinutes(_options.AccessTokenLifetimeMinutes),
            SigningCredentials = credentials
        };

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(descriptor);

        return handler.WriteToken(token);
    }
}
```

- [ ] **Step 4: Run the JWT generator test again**

Run: `dotnet test tests/Todo.Api.UnitTests/Todo.Api.UnitTests.csproj --filter JwtTokenGeneratorTests -v minimal`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/Todo.Api/Todo.Api.csproj src/Todo.Api/Abstractions/Security/IJwtTokenGenerator.cs src/Todo.Api/Security/Jwt tests/Todo.Api.UnitTests/Security/Jwt/JwtTokenGeneratorTests.cs
git commit -m "feat: add jwt token generation"
```

## Task 2: Extend Password Verification

**Files:**

- Modify: `src/Todo.Api/Abstractions/Security/IPasswordHasher.cs`
- Modify: `src/Todo.Api/Security/PasswordHasher.cs`
- Modify: `tests/Todo.Api.UnitTests/Security/PasswordHasherTests.cs`

- [ ] **Step 1: Add failing password verification tests**

```csharp
// tests/Todo.Api.UnitTests/Security/PasswordHasherTests.cs
using FluentAssertions;
using Todo.Api.Security;

namespace Todo.Api.UnitTests.Security;

public sealed class PasswordHasherTests
{
    [Fact]
    public void Hash_Should_Return_Derived_Value_Instead_Of_Raw_Password()
    {
        var hasher = new PasswordHasher();

        var hash = hasher.Hash("secret123");

        hash.Should().NotBe("secret123");
        hash.Should().Contain(".");
    }

    [Fact]
    public void Verify_Should_Return_True_For_Correct_Password()
    {
        var hasher = new PasswordHasher();
        var hash = hasher.Hash("secret123");

        var isValid = hasher.Verify("secret123", hash);

        isValid.Should().BeTrue();
    }

    [Fact]
    public void Verify_Should_Return_False_For_Incorrect_Password()
    {
        var hasher = new PasswordHasher();
        var hash = hasher.Hash("secret123");

        var isValid = hasher.Verify("wrong-password", hash);

        isValid.Should().BeFalse();
    }
}
```

- [ ] **Step 2: Run the password hasher tests to confirm they fail**

Run: `dotnet test tests/Todo.Api.UnitTests/Todo.Api.UnitTests.csproj --filter PasswordHasherTests -v minimal`
Expected: FAIL because `IPasswordHasher.Verify(...)` does not exist yet.

- [ ] **Step 3: Add verification support without changing the stored hash format**

```csharp
// src/Todo.Api/Abstractions/Security/IPasswordHasher.cs
namespace Todo.Api.Abstractions.Security;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}
```

```csharp
// src/Todo.Api/Security/PasswordHasher.cs
using System.Security.Cryptography;
using Todo.Api.Abstractions.Security;

namespace Todo.Api.Security;

public sealed class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            KeySize);

        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string passwordHash)
    {
        var segments = passwordHash.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length != 3 || !int.TryParse(segments[0], out var iterations))
        {
            return false;
        }

        byte[] salt;
        byte[] expectedHash;

        try
        {
            salt = Convert.FromBase64String(segments[1]);
            expectedHash = Convert.FromBase64String(segments[2]);
        }
        catch (FormatException)
        {
            return false;
        }

        var actualHash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            expectedHash.Length);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}
```

- [ ] **Step 4: Run the password hasher tests again**

Run: `dotnet test tests/Todo.Api.UnitTests/Todo.Api.UnitTests.csproj --filter PasswordHasherTests -v minimal`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/Todo.Api/Abstractions/Security/IPasswordHasher.cs src/Todo.Api/Security/PasswordHasher.cs tests/Todo.Api.UnitTests/Security/PasswordHasherTests.cs
git commit -m "feat: add password verification"
```

## Task 3: Add the Sign-In Slice and Handler Tests

**Files:**

- Create: `src/Todo.Api/Features/Auth/SignIn/Command.cs`
- Create: `src/Todo.Api/Features/Auth/SignIn/Response.cs`
- Create: `src/Todo.Api/Features/Auth/SignIn/Validator.cs`
- Create: `src/Todo.Api/Features/Auth/SignIn/Handler.cs`
- Modify: `src/Todo.Api/Features/Auth/AuthErrors.cs`
- Create: `tests/Todo.Api.UnitTests/Features/Auth/SignIn/SignInValidatorTests.cs`
- Create: `tests/Todo.Api.UnitTests/Features/Auth/SignIn/SignInHandlerTests.cs`

- [ ] **Step 1: Write the failing validator and handler tests**

```csharp
// tests/Todo.Api.UnitTests/Features/Auth/SignIn/SignInValidatorTests.cs
using FluentAssertions;
using Todo.Api.Features.Auth.SignIn;

namespace Todo.Api.UnitTests.Features.Auth.SignIn;

public sealed class SignInValidatorTests
{
    private readonly Validator _validator = new();

    [Fact]
    public void Validate_Should_Fail_When_Email_Is_Missing()
    {
        var result = _validator.Validate(new Command(string.Empty, "secret123"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(Command.Email));
    }

    [Fact]
    public void Validate_Should_Fail_When_Email_Is_Invalid()
    {
        var result = _validator.Validate(new Command("not-an-email", "secret123"));

        result.Errors.Should().Contain(x => x.PropertyName == nameof(Command.Email));
    }

    [Fact]
    public void Validate_Should_Fail_When_Password_Is_Missing()
    {
        var result = _validator.Validate(new Command("user@example.com", string.Empty));

        result.Errors.Should().Contain(x => x.PropertyName == nameof(Command.Password));
    }
}
```

```csharp
// tests/Todo.Api.UnitTests/Features/Auth/SignIn/SignInHandlerTests.cs
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
```

- [ ] **Step 2: Run the sign-in unit tests to confirm they fail**

Run: `dotnet test tests/Todo.Api.UnitTests/Todo.Api.UnitTests.csproj --filter "SignInValidatorTests|SignInHandlerTests" -v minimal`
Expected: FAIL because the sign-in slice does not exist yet.

- [ ] **Step 3: Implement the sign-in command, response, validator, handler, and auth error**

```csharp
// src/Todo.Api/Features/Auth/AuthErrors.cs
using BuildingBlocks.Application.Results;

namespace Todo.Api.Features.Auth;

public static class AuthErrors
{
    public static readonly Error DuplicateEmail =
        new("auth.duplicate_email", "The email address is already in use.", ErrorType.Conflict);

    public static readonly Error DuplicateUserName =
        new("auth.duplicate_user_name", "The user name is already in use.", ErrorType.Conflict);

    public static readonly Error InvalidCredentials =
        new("auth.invalid_credentials", "The email or password is incorrect.", ErrorType.Unauthorized);
}
```

```csharp
// src/Todo.Api/Features/Auth/SignIn/Command.cs
using BuildingBlocks.Application.Results;
using MediatR;

namespace Todo.Api.Features.Auth.SignIn;

public sealed record Command(string Email, string Password) : IRequest<Result<Response>>;
```

```csharp
// src/Todo.Api/Features/Auth/SignIn/Response.cs
namespace Todo.Api.Features.Auth.SignIn;

public sealed record Response(string AccessToken);
```

```csharp
// src/Todo.Api/Features/Auth/SignIn/Validator.cs
using FluentValidation;

namespace Todo.Api.Features.Auth.SignIn;

public sealed class Validator : AbstractValidator<Command>
{
    public Validator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty();
    }
}
```

```csharp
// src/Todo.Api/Features/Auth/SignIn/Handler.cs
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
```

- [ ] **Step 4: Run the sign-in unit tests again**

Run: `dotnet test tests/Todo.Api.UnitTests/Todo.Api.UnitTests.csproj --filter "SignInValidatorTests|SignInHandlerTests" -v minimal`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/Todo.Api/Features/Auth tests/Todo.Api.UnitTests/Features/Auth/SignIn
git commit -m "feat: add sign-in application flow"
```

## Task 4: Wire JWT Auth and Endpoint Registration

**Files:**

- Modify: `src/Todo.Api/Extensions/ServiceCollectionExtensions.cs`
- Modify: `src/Todo.Api/Extensions/ApplicationExtensions.cs`
- Create: `src/Todo.Api/Features/Auth/SignIn/Endpoint.cs`
- Modify: `src/Todo.Api/appsettings.json`
- Modify: `src/Todo.Api/appsettings.Development.json`

- [ ] **Step 1: Write the failing integration test for invalid credentials**

```csharp
// tests/Todo.Api.IntegrationTests/Features/Auth/SignIn/SignInEndpointsTests.cs
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Todo.Api.IntegrationTests.Fixtures;

namespace Todo.Api.IntegrationTests.Features.Auth.SignIn;

public sealed partial class SignInEndpointsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
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
        problem!.Title.Should().Be("Unauthorized");
        problem.Type.Should().Be("auth.invalid_credentials");
        problem.Detail.Should().Be("The email or password is incorrect.");
    }
}
```

- [ ] **Step 2: Run the integration test to confirm it fails**

Run: `dotnet test tests/Todo.Api.IntegrationTests/Todo.Api.IntegrationTests.csproj --filter Post_SignIn_Should_Return_Unauthorized_For_Unknown_Email -v minimal`
Expected: FAIL because `/api/v1/auth/sign-in` is not registered yet.

- [ ] **Step 3: Register JWT services, add middleware, add the endpoint, and bind configuration**

```csharp
// src/Todo.Api/Extensions/ServiceCollectionExtensions.cs
using System.Text;
using BuildingBlocks.Application.Behaviors;
using BuildingBlocks.Endpoints;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Todo.Api.Abstractions.Persistence;
using Todo.Api.Abstractions.Security;
using Todo.Api.Features.Sample.Create;
using Todo.Api.Persistence;
using Todo.Api.Security;
using Todo.Api.Security.Jwt;

namespace Todo.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        var apiAssembly = typeof(Command).Assembly;

        services.AddProblemDetails();
        services.AddOpenApi();
        services.AddHealthChecks();
        services.AddEndpoints(apiAssembly);

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName));

        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? new JwtOptions();

        if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey))
        {
            throw new InvalidOperationException("Jwt:SigningKey was not found.");
        }

        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Connection string 'Postgres' was not found.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));
        services.AddScoped<IApplicationDbContext>(serviceProvider =>
            serviceProvider.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization();

        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(apiAssembly);
            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
            configuration.AddOpenBehavior(typeof(RequestLoggingBehavior<,>));
        });
        services.AddValidatorsFromAssembly(apiAssembly);

        return services;
    }
}
```

```csharp
// src/Todo.Api/Extensions/ApplicationExtensions.cs
namespace Todo.Api.Extensions;

public static class ApplicationExtensions
{
    public static WebApplication UseApplicationPipeline(this WebApplication app)
    {
        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }
}
```

```csharp
// src/Todo.Api/Features/Auth/SignIn/Endpoint.cs
using BuildingBlocks.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Routing;
using Todo.Api.ExceptionHandling;

namespace Todo.Api.Features.Auth.SignIn;

public sealed class Endpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/sign-in", async (Command command, ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(command, cancellationToken);

                if (result.IsSuccess)
                {
                    return Results.Ok(result.Value);
                }

                var problemDetails = ErrorProblemDetailsMapper.Map(result.Error!);

                return Results.Problem(
                    title: problemDetails.Title,
                    type: problemDetails.Type,
                    detail: problemDetails.Detail,
                    statusCode: problemDetails.Status);
            })
            .WithName("AuthSignIn")
            .WithTags("Auth");
    }
}
```

```json
// src/Todo.Api/appsettings.json
{
  "ConnectionStrings": {
    "Postgres": "Server=localhost;Port=5433;Database=superpowerai;User Id=tuantai3001;Password=01223326833Tt;Pooling=true; Include Error Detail=true"
  },
  "Jwt": {
    "Issuer": "Todo.Api",
    "Audience": "Todo.Api.Client",
    "SigningKey": "development-signing-key-change-me-1234567890",
    "AccessTokenLifetimeMinutes": 60
  },
  "Serilog": {
    "Using": [ "Serilog.Sinks.Console" ],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.AspNetCore": "Warning"
      }
    },
    "WriteTo": [ { "Name": "Console" } ],
    "Enrich": [ "FromLogContext" ]
  }
}
```

```json
// src/Todo.Api/appsettings.Development.json
{
  "Jwt": {
    "Issuer": "Todo.Api",
    "Audience": "Todo.Api.Client",
    "SigningKey": "development-signing-key-change-me-1234567890",
    "AccessTokenLifetimeMinutes": 60
  }
}
```

- [ ] **Step 4: Run the unauthorized integration test again**

Run: `dotnet test tests/Todo.Api.IntegrationTests/Todo.Api.IntegrationTests.csproj --filter Post_SignIn_Should_Return_Unauthorized_For_Unknown_Email -v minimal`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/Todo.Api/Extensions src/Todo.Api/Features/Auth/SignIn/Endpoint.cs src/Todo.Api/appsettings.json src/Todo.Api/appsettings.Development.json
git commit -m "feat: wire jwt authentication and sign-in endpoint"
```

## Task 5: Add Full Sign-In Integration Coverage

**Files:**

- Create: `tests/Todo.Api.IntegrationTests/Features/Auth/SignIn/SignInEndpointsTests.cs`

- [ ] **Step 1: Expand the integration tests to cover valid sign-in, JWT claims, and wrong-password behavior**

```csharp
// tests/Todo.Api.IntegrationTests/Features/Auth/SignIn/SignInEndpointsTests.cs
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();
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
        problem!.Type.Should().Be("auth.invalid_credentials");
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
```

- [ ] **Step 2: Run the integration test project**

Run: `dotnet test tests/Todo.Api.IntegrationTests/Todo.Api.IntegrationTests.csproj -v minimal`
Expected: PASS

- [ ] **Step 3: Commit**

```bash
git add tests/Todo.Api.IntegrationTests/Features/Auth/SignIn/SignInEndpointsTests.cs
git commit -m "test: add sign-in integration coverage"
```

## Task 6: Final Verification

**Files:**

- Modify: any touched file as needed for final fixes

- [ ] **Step 1: Run the full solution test suite**

Run: `dotnet test tests/Todo.Api.UnitTests/Todo.Api.UnitTests.csproj -v minimal`
Expected: PASS

Run: `dotnet test tests/Todo.Api.IntegrationTests/Todo.Api.IntegrationTests.csproj -v minimal`
Expected: PASS

- [ ] **Step 2: Run the API and verify the endpoint manually**

Run: `dotnet run --project src/Todo.Api/Todo.Api.csproj`
Expected: the API starts successfully and exposes `/api/v1/auth/sign-in`.

Run:

```powershell
curl.exe -i -X POST http://localhost:5000/api/v1/auth/sign-in `
  -H "Content-Type: application/json" `
  -d "{\"email\":\"manual@example.com\",\"password\":\"secret123\"}"
```

Expected: `HTTP/1.1 200 OK` with JSON shaped like `{"accessToken":"<jwt>"}` when the user exists, or `HTTP/1.1 401 Unauthorized` with `type` equal to `auth.invalid_credentials` otherwise.

- [ ] **Step 3: Commit the finished feature**

```bash
git add src tests
git commit -m "feat: add user sign-in jwt flow"
```

## Self-Review

- Spec coverage: the plan covers the sign-in endpoint, request validation, password verification, generic unauthorized failures, JWT creation with `sub`/`email`/`unique_name`, configuration-driven lifetime with a 60-minute default, centralized auth wiring, and unit plus integration coverage.
- Placeholder scan: no `TODO`, `TBD`, or `implement later` placeholders remain.
- Type consistency: the plan consistently uses `IPasswordHasher.Verify`, `IJwtTokenGenerator.Generate`, `JwtOptions`, `AuthErrors.InvalidCredentials`, `SignIn.Command`, `SignIn.Response`, and `/api/v1/auth/sign-in`.
