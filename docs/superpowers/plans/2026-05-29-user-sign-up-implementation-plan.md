# User Sign-Up Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** Add `POST /api/v1/auth/sign-up` with validation, password hashing, uniqueness enforcement, PostgreSQL persistence, and `201 Created` / `409 Conflict` behavior.

**Architecture:** This revision keeps the sign-up feature and all runtime persistence concerns inside `Todo.Api`. `User`, `IPasswordHasher`, `ApplicationDbContext`, EF Core configurations, migrations, and the custom `PasswordHasher` live in `Todo.Api`, and `User.Register(...)` is a simple factory without domain-level guard clauses. Validation stays in FluentValidation and uniqueness stays in the handler plus database indexes.

**Tech Stack:** .NET 10, ASP.NET Core Minimal API, MediatR, FluentValidation, EF Core 10, Npgsql, custom PBKDF2 password hashing, xUnit, FluentAssertions, NSubstitute, Microsoft.AspNetCore.Mvc.Testing, Testcontainers.PostgreSql

---

## File Structure

- `src/Todo.Api/Domain/Users/User.cs`
  Responsibility: user entity and simple registration factory.
- `src/Todo.Api/Abstractions/Security/IPasswordHasher.cs`
  Responsibility: password hashing contract used by the sign-up handler.
- `src/Todo.Api/Security/PasswordHasher.cs`
  Responsibility: custom PBKDF2-based password hashing implementation.
- `src/Todo.Api/Features/Auth/AuthErrors.cs`
  Responsibility: duplicate email and duplicate username errors.
- `src/Todo.Api/Features/Auth/SignUp/Command.cs`
- `src/Todo.Api/Features/Auth/SignUp/Response.cs`
- `src/Todo.Api/Features/Auth/SignUp/Validator.cs`
- `src/Todo.Api/Features/Auth/SignUp/Handler.cs`
- `src/Todo.Api/Features/Auth/SignUp/Endpoint.cs`
  Responsibility: vertical slice for sign-up.
- `src/Todo.Api/Abstractions/Persistence/IApplicationDbContext.cs`
  Responsibility: expose `DbSet<User> Users`.
- `src/Todo.Api/Persistence/ApplicationDbContext.cs`
  Responsibility: EF Core DbContext with `Users`.
- `src/Todo.Api/Persistence/Configurations/UserConfiguration.cs`
  Responsibility: EF Core mapping and unique indexes for users.
- `src/Todo.Api/Extensions/ServiceCollectionExtensions.cs`
  Responsibility: register `ApplicationDbContext`, the custom `PasswordHasher`, and the application services.
- `src/Todo.Api/Persistence/Migrations/*AddUsers*.cs`
- `src/Todo.Api/Persistence/Migrations/ApplicationDbContextModelSnapshot.cs`
  Responsibility: create the `users` table and indexes.
- `tests/Todo.Api.UnitTests/Security/PasswordHasherTests.cs`
  Responsibility: verify the custom hasher produces a derived value instead of the raw password.
- `tests/Todo.Api.UnitTests/Features/Auth/SignUp/SignUpValidatorTests.cs`
- `tests/Todo.Api.UnitTests/Features/Auth/SignUp/SignUpHandlerTests.cs`
  Responsibility: validator and handler coverage.
- `tests/Todo.Api.IntegrationTests/Features/Auth/SignUp/SignUpEndpointsTests.cs`
  Responsibility: end-to-end API and persistence verification.

## Task 1: Add the User and Custom Password Hasher

**Files:**

- Create: `src/Todo.Api/Domain/Users/User.cs`
- Create: `src/Todo.Api/Abstractions/Security/IPasswordHasher.cs`
- Create: `src/Todo.Api/Security/PasswordHasher.cs`

- [x] **Step 1: Add the user entity with a simple factory**

```csharp
// src/Todo.Api/Domain/Users/User.cs
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
```

- [x] **Step 2: Add the password hashing abstraction and custom implementation**

```csharp
// src/Todo.Api/Abstractions/Security/IPasswordHasher.cs
namespace Todo.Api.Abstractions.Security;

public interface IPasswordHasher
{
    string Hash(string password);
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
}
```

- [x] **Step 3: Run a build to verify the new domain and security files compile**

Run: `dotnet build SuperPowerAI.sln`
Expected: `Build succeeded.`

- [x] **Step 4: Commit**

```bash
git add src/Todo.Api/Domain src/Todo.Api/Abstractions/Security src/Todo.Api/Security
git commit -m "feat: add user entity and custom password hasher"
```

## Task 2: Add Password Hasher Unit Coverage

**Files:**

- Create: `tests/Todo.Api.UnitTests/Security/PasswordHasherTests.cs`

- [x] **Step 1: Write the failing password hasher test**

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
        hash.Should().Contain('.');
    }
}
```

- [x] **Step 2: Run the password hasher test to confirm it fails**

Run: `dotnet test tests/Todo.Api.UnitTests/Todo.Api.UnitTests.csproj --filter PasswordHasherTests -v minimal`
Expected: FAIL because `PasswordHasher` does not exist yet

- [x] **Step 3: Run the password hasher test again**

Run: `dotnet test tests/Todo.Api.UnitTests/Todo.Api.UnitTests.csproj --filter PasswordHasherTests -v minimal`
Expected: PASS

- [x] **Step 4: Commit**

```bash
git add tests/Todo.Api.UnitTests/Security/PasswordHasherTests.cs src/Todo.Api/Abstractions/Security src/Todo.Api/Security
git commit -m "test: add password hasher coverage"
```

## Task 3: Add User Persistence and DI Wiring Inside `Todo.Api`

**Files:**

- Modify: `src/Todo.Api/Abstractions/Persistence/IApplicationDbContext.cs`
- Modify: `src/Todo.Api/Persistence/ApplicationDbContext.cs`
- Create: `src/Todo.Api/Persistence/Configurations/UserConfiguration.cs`
- Modify: `src/Todo.Api/Extensions/ServiceCollectionExtensions.cs`
- Modify: `src/Todo.Api/Persistence/Migrations/ApplicationDbContextModelSnapshot.cs`
- Create: `src/Todo.Api/Persistence/Migrations/*AddUsers*.cs`

- [x] **Step 1: Expose users through the DbContext abstraction and implementation**

```csharp
// src/Todo.Api/Abstractions/Persistence/IApplicationDbContext.cs
using Microsoft.EntityFrameworkCore;
using Todo.Api.Domain.Users;
using Todo.Api.Persistence.Models;

namespace Todo.Api.Abstractions.Persistence;

public interface IApplicationDbContext
{
    DbSet<SampleItem> SampleItems { get; }
    DbSet<User> Users { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

```csharp
// src/Todo.Api/Persistence/ApplicationDbContext.cs
using Microsoft.EntityFrameworkCore;
using Todo.Api.Abstractions.Persistence;
using Todo.Api.Domain.Users;
using Todo.Api.Persistence.Models;

namespace Todo.Api.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<SampleItem> SampleItems => Set<SampleItem>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
```

- [x] **Step 2: Add the EF Core mapping**

```csharp
// src/Todo.Api/Persistence/Configurations/UserConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Todo.Api.Domain.Users;

namespace Todo.Api.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Email)
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(x => x.UserName)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.PasswordHash)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(x => x.Email).IsUnique();
        builder.HasIndex(x => x.UserName).IsUnique();
    }
}
```

- [x] **Step 3: Register the custom password hasher**

```csharp
// src/Todo.Api/Extensions/ServiceCollectionExtensions.cs
using BuildingBlocks.Application.Behaviors;
using BuildingBlocks.Endpoints;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Todo.Api.Abstractions.Persistence;
using Todo.Api.Abstractions.Security;
using Todo.Api.Features.Sample.Create;
using Todo.Api.Persistence;
using Todo.Api.Security;

namespace Todo.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        var apiAssembly = typeof(Command).Assembly;
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Connection string 'Postgres' was not found.");

        services.AddProblemDetails();
        services.AddOpenApi();
        services.AddHealthChecks();
        services.AddEndpoints(apiAssembly);
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));
        services.AddScoped<IApplicationDbContext>(serviceProvider =>
            serviceProvider.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IPasswordHasher, PasswordHasher>();
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

- [x] **Step 4: Generate the users migration**

Run:

```bash
dotnet ef migrations add AddUsers --project src/Todo.Api/Todo.Api.csproj --startup-project src/Todo.Api/Todo.Api.csproj --output-dir Persistence/Migrations
```

Expected: EF creates `*AddUsers.cs`, `*AddUsers.Designer.cs`, and updates `src/Todo.Api/Persistence/Migrations/ApplicationDbContextModelSnapshot.cs`.

- [x] **Step 5: Run a build to verify persistence wiring**

Run: `dotnet build SuperPowerAI.sln`
Expected: `Build succeeded.`

- [x] **Step 6: Commit**

```bash
git add src/Todo.Api
git commit -m "feat: add user persistence and di wiring"
```

## Task 4: Implement the Sign-Up Slice

**Files:**

- Create: `src/Todo.Api/Features/Auth/AuthErrors.cs`
- Create: `src/Todo.Api/Features/Auth/SignUp/Command.cs`
- Create: `src/Todo.Api/Features/Auth/SignUp/Response.cs`
- Create: `src/Todo.Api/Features/Auth/SignUp/Validator.cs`
- Create: `src/Todo.Api/Features/Auth/SignUp/Handler.cs`
- Create: `src/Todo.Api/Features/Auth/SignUp/Endpoint.cs`

- [x] **Step 1: Write the failing validator test**

```csharp
// tests/Todo.Api.UnitTests/Features/Auth/SignUp/SignUpValidatorTests.cs
using FluentAssertions;
using Todo.Api.Features.Auth.SignUp;

namespace Todo.Api.UnitTests.Features.Auth.SignUp;

public sealed class SignUpValidatorTests
{
    [Fact]
    public void Validate_Should_Fail_When_Email_Is_Missing()
    {
        var validator = new Validator();
        var result = validator.Validate(new Command(string.Empty, "secret123", "sampleUser"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(Command.Email));
    }
}
```

- [x] **Step 2: Run the validator test to confirm it fails**

Run: `dotnet test tests/Todo.Api.UnitTests/Todo.Api.UnitTests.csproj --filter SignUpValidatorTests -v minimal`
Expected: FAIL because the sign-up slice does not exist yet

- [x] **Step 3: Add auth errors, command, response, and validator**

```csharp
// src/Todo.Api/Features/Auth/AuthErrors.cs
using BuildingBlocks.Application.Results;

namespace Todo.Api.Features.Auth;

public static class AuthErrors
{
    public static readonly Error DuplicateEmail = new("auth.duplicate_email", "The email address is already in use.");
    public static readonly Error DuplicateUserName = new("auth.duplicate_user_name", "The user name is already in use.");
}
```

```csharp
// src/Todo.Api/Features/Auth/SignUp/Command.cs
using BuildingBlocks.Application.Results;
using MediatR;

namespace Todo.Api.Features.Auth.SignUp;

public sealed record Command(string Email, string Password, string UserName) : IRequest<Result<Response>>;
```

```csharp
// src/Todo.Api/Features/Auth/SignUp/Response.cs
namespace Todo.Api.Features.Auth.SignUp;

public sealed record Response(Guid Id, string Email, string UserName);
```

```csharp
// src/Todo.Api/Features/Auth/SignUp/Validator.cs
using FluentValidation;

namespace Todo.Api.Features.Auth.SignUp;

public sealed class Validator : AbstractValidator<Command>
{
    public Validator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8);

        RuleFor(x => x.UserName)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(30);
    }
}
```

- [x] **Step 4: Implement the handler**

```csharp
// src/Todo.Api/Features/Auth/SignUp/Handler.cs
using BuildingBlocks.Application.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Todo.Api.Abstractions.Security;
using Todo.Api.Abstractions.Persistence;
using Todo.Api.Domain.Users;
using Todo.Api.Features.Auth;

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
            return Result<Response>.Failure(AuthErrors.DuplicateEmail);
        }

        if (await dbContext.Users.AnyAsync(x => x.UserName == userName, cancellationToken))
        {
            return Result<Response>.Failure(AuthErrors.DuplicateUserName);
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
            return postgresException.ConstraintName switch
            {
                "IX_users_Email" => Result<Response>.Failure(AuthErrors.DuplicateEmail),
                "IX_users_UserName" => Result<Response>.Failure(AuthErrors.DuplicateUserName),
                _ => throw
            };
        }

        return Result<Response>.Success(new Response(user.Id, user.Email, user.UserName));
    }
}
```

- [x] **Step 5: Implement the endpoint**

```csharp
// src/Todo.Api/Features/Auth/SignUp/Endpoint.cs
using BuildingBlocks.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Todo.Api.Features.Auth;

namespace Todo.Api.Features.Auth.SignUp;

public sealed class Endpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/sign-up", async (Command command, ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(command, cancellationToken);

                if (result.IsSuccess)
                {
                    return Results.Created("/api/v1/auth/sign-up", result.Value);
                }

                var statusCode = result.Error == AuthErrors.DuplicateEmail || result.Error == AuthErrors.DuplicateUserName
                    ? StatusCodes.Status409Conflict
                    : StatusCodes.Status400BadRequest;

                return Results.Problem(
                    statusCode: statusCode,
                    title: statusCode == StatusCodes.Status409Conflict ? "Duplicate user field." : "Request failed",
                    detail: result.Error!.Message,
                    type: result.Error.Code);
            })
            .WithName("AuthSignUp")
            .WithTags("Auth");
    }
}
```

- [x] **Step 6: Expand the validator test suite and rerun it**

```csharp
// tests/Todo.Api.UnitTests/Features/Auth/SignUp/SignUpValidatorTests.cs
using FluentAssertions;
using Todo.Api.Features.Auth.SignUp;

namespace Todo.Api.UnitTests.Features.Auth.SignUp;

public sealed class SignUpValidatorTests
{
    private readonly Validator _validator = new();

    [Fact]
    public void Validate_Should_Fail_When_Email_Is_Missing()
    {
        var result = _validator.Validate(new Command(string.Empty, "secret123", "sampleUser"));
        result.Errors.Should().Contain(x => x.PropertyName == nameof(Command.Email));
    }

    [Fact]
    public void Validate_Should_Fail_When_Email_Is_Invalid()
    {
        var result = _validator.Validate(new Command("not-an-email", "secret123", "sampleUser"));
        result.Errors.Should().Contain(x => x.PropertyName == nameof(Command.Email));
    }

    [Fact]
    public void Validate_Should_Fail_When_Password_Is_Too_Short()
    {
        var result = _validator.Validate(new Command("user@example.com", "short", "sampleUser"));
        result.Errors.Should().Contain(x => x.PropertyName == nameof(Command.Password));
    }

    [Fact]
    public void Validate_Should_Fail_When_UserName_Is_Too_Short()
    {
        var result = _validator.Validate(new Command("user@example.com", "secret123", "ab"));
        result.Errors.Should().Contain(x => x.PropertyName == nameof(Command.UserName));
    }

    [Fact]
    public void Validate_Should_Fail_When_UserName_Is_Too_Long()
    {
        var result = _validator.Validate(new Command("user@example.com", "secret123", new string('a', 31)));
        result.Errors.Should().Contain(x => x.PropertyName == nameof(Command.UserName));
    }
}
```

Run: `dotnet test tests/Todo.Api.UnitTests/Todo.Api.UnitTests.csproj --filter SignUpValidatorTests -v minimal`
Expected: PASS

- [x] **Step 7: Commit**

```bash
git add src/Todo.Api tests/Todo.Api.UnitTests/Features/Auth/SignUp/SignUpValidatorTests.cs
git commit -m "feat: add sign-up vertical slice"
```

## Task 5: Add Handler Unit Tests

**Files:**

- Create: `tests/Todo.Api.UnitTests/Features/Auth/SignUp/SignUpHandlerTests.cs`

- [x] **Step 1: Write the handler tests**

```csharp
// tests/Todo.Api.UnitTests/Features/Auth/SignUp/SignUpHandlerTests.cs
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
```

- [x] **Step 2: Run the full unit-test project**

Run: `dotnet test tests/Todo.Api.UnitTests/Todo.Api.UnitTests.csproj -v minimal`
Expected: PASS

- [x] **Step 3: Commit**

```bash
git add tests/Todo.Api.UnitTests
git commit -m "test: add sign-up handler tests"
```

## Task 6: Add PostgreSQL Integration Tests

**Files:**

- Create: `tests/Todo.Api.IntegrationTests/Features/Auth/SignUp/SignUpEndpointsTests.cs`

- [x] **Step 1: Write the end-to-end tests**

```csharp
// tests/Todo.Api.IntegrationTests/Features/Auth/SignUp/SignUpEndpointsTests.cs
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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

        response.StatusCode.Should().Be(HttpStatusCode.Created);

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

        json.Should().NotContain("password", StringComparison.OrdinalIgnoreCase);
        json.Should().NotContain("passwordHash", StringComparison.OrdinalIgnoreCase);
    }

    private sealed record SignUpResponse(Guid Id, string Email, string UserName);
}
```

- [x] **Step 2: Run the integration test project**

Run: `dotnet test tests/Todo.Api.IntegrationTests/Todo.Api.IntegrationTests.csproj -v minimal`
Expected: PASS

- [x] **Step 3: Commit**

```bash
git add tests/Todo.Api.IntegrationTests
git commit -m "test: add sign-up integration tests"
```

## Task 7: Final Verification

**Files:**

- Modify: any touched file as needed for final fixes

- [x] **Step 1: Run the full solution test suite**

Run: `dotnet test SuperPowerAI.sln -v minimal`
Expected: all unit and integration tests PASS

- [x] **Step 2: Run the API and verify the endpoint manually**

Run: `dotnet run --project src/Todo.Api/Todo.Api.csproj`
Expected: the API starts successfully and exposes `/api/v1/auth/sign-up`

Run:

```powershell
curl.exe -i -X POST http://localhost:5000/api/v1/auth/sign-up `
  -H "Content-Type: application/json" `
  -d "{\"email\":\"manual@example.com\",\"password\":\"secret123\",\"userName\":\"manualUser\"}"
```

Expected: `HTTP/1.1 201 Created` with JSON containing `id`, `email`, and `userName`, and no password fields

- [x] **Step 3: Commit the finished feature**

```bash
git add src tests
git commit -m "feat: add user sign-up flow"
```

## Self-Review

- Spec coverage: the plan still covers the sign-up endpoint, validation, uniqueness, hashing, password-hasher unit coverage, persistence, migration, `201 Created`, `409 Conflict`, and database-race translation.
- Placeholder scan: no `TODO`, `TBD`, or deferred sections remain.
- Type consistency: the plan consistently uses `Todo.Api.Domain.Users.User`, `Todo.Api.Abstractions.Security.IPasswordHasher`, `PasswordHasher`, `CreatedAtUtc`, `AuthErrors`, `Command`, `Response`, and `/api/v1/auth/sign-up`.
