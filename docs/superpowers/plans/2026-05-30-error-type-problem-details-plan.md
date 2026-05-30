# ErrorType ProblemDetails Mapping Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a full `ErrorType` taxonomy to application errors and route those error types through a shared base .NET `ProblemDetails` mapper so endpoints stop comparing specific error instances inline.

**Architecture:** Keep transport-agnostic error metadata in `BuildingBlocks.Application.Results` by adding an `ErrorType` enum and storing it on `Error`. Keep HTTP concerns in `Todo.Api` by translating `ErrorType` to `ProblemDetails` status/title in a reusable mapper under `Todo.Api.ExceptionHandling`, while continuing to use `Microsoft.AspNetCore.Mvc.ProblemDetails` as the response shape.

**Tech Stack:** .NET 10, ASP.NET Core Minimal APIs, xUnit, FluentAssertions

---

### Task 1: Lock Error Metadata Expectations with Unit Tests

**Files:**
- Modify: `tests/Todo.Api.UnitTests/Features/Auth/SignUp/SignUpHandlerTests.cs`

- [ ] **Step 1: Write the failing test assertions for error types**

Update the two conflict-path tests so they assert the returned error type as well as the specific error instance.

```csharp
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
    result.Error!.Type.Should().Be(ErrorType.Conflict);
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
    result.Error!.Type.Should().Be(ErrorType.Conflict);
}
```

- [ ] **Step 2: Run the unit tests to verify they fail**

Run: `dotnet test tests/Todo.Api.UnitTests/Todo.Api.UnitTests.csproj --filter "FullyQualifiedName~SignUpHandlerTests"`

Expected: FAIL with compile errors because `Error` does not expose `Type` and `ErrorType` does not exist yet.

- [ ] **Step 3: Commit the red test state if working in a dedicated branch that tracks red-green commits**

```bash
git add tests/Todo.Api.UnitTests/Features/Auth/SignUp/SignUpHandlerTests.cs
git commit -m "test: define error type expectations"
```

### Task 2: Add Full ErrorType Support in BuildingBlocks and Auth Errors

**Files:**
- Create: `src/BuildingBlocks/Application/Results/ErrorType.cs`
- Modify: `src/BuildingBlocks/Application/Results/Error.cs`
- Modify: `src/Todo.Api/Features/Auth/AuthErrors.cs`

- [ ] **Step 1: Create the shared `ErrorType` enum**

Create `src/BuildingBlocks/Application/Results/ErrorType.cs` with the full application-level taxonomy.

```csharp
namespace BuildingBlocks.Application.Results;

public enum ErrorType
{
    Failure = 0,
    Validation = 1,
    Conflict = 2,
    NotFound = 3,
    Unauthorized = 4,
    Forbidden = 5,
    Unexpected = 6
}
```

- [ ] **Step 2: Update `Error` to carry `ErrorType` and assign defaults to shared errors**

Replace `src/BuildingBlocks/Application/Results/Error.cs` with:

```csharp
namespace BuildingBlocks.Application.Results;

public sealed record Error(string Code, string Message, ErrorType Type)
{
    public static readonly Error Failure = new("common.failure", "The request failed.", ErrorType.Failure);
    public static readonly Error Validation = new("common.validation", "The request failed validation.", ErrorType.Validation);
    public static readonly Error NotFound = new("common.not_found", "The requested resource was not found.", ErrorType.NotFound);
    public static readonly Error Unauthorized = new("common.unauthorized", "The current user is not authenticated.", ErrorType.Unauthorized);
    public static readonly Error Forbidden = new("common.forbidden", "The current user does not have access to this resource.", ErrorType.Forbidden);
    public static readonly Error Unexpected = new("common.unexpected", "An unexpected error occurred.", ErrorType.Unexpected);
}
```

- [ ] **Step 3: Update auth-specific errors to classify duplicates as conflicts**

Replace `src/Todo.Api/Features/Auth/AuthErrors.cs` with:

```csharp
using BuildingBlocks.Application.Results;

namespace Todo.Api.Features.Auth;

public static class AuthErrors
{
    public static readonly Error DuplicateEmail =
        new("auth.duplicate_email", "The email address is already in use.", ErrorType.Conflict);

    public static readonly Error DuplicateUserName =
        new("auth.duplicate_user_name", "The user name is already in use.", ErrorType.Conflict);
}
```

- [ ] **Step 4: Run the handler unit tests to verify the new metadata passes**

Run: `dotnet test tests/Todo.Api.UnitTests/Todo.Api.UnitTests.csproj --filter "FullyQualifiedName~SignUpHandlerTests"`

Expected: PASS

- [ ] **Step 5: Commit the application error metadata change**

```bash
git add src/BuildingBlocks/Application/Results/ErrorType.cs src/BuildingBlocks/Application/Results/Error.cs src/Todo.Api/Features/Auth/AuthErrors.cs tests/Todo.Api.UnitTests/Features/Auth/SignUp/SignUpHandlerTests.cs
git commit -m "feat: add application error types"
```

### Task 3: Lock the API ProblemDetails Contract with Integration Tests

**Files:**
- Modify: `tests/Todo.Api.IntegrationTests/Features/Auth/SignUp/SignUpEndpointsTests.cs`

- [ ] **Step 1: Write the failing integration assertions for `ProblemDetails`**

Add `using Microsoft.AspNetCore.Mvc;` at the top of the file, then strengthen the duplicate-email and duplicate-user-name tests so they deserialize base .NET `ProblemDetails` and verify the mapped metadata.

```csharp
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
```

- [ ] **Step 2: Run the integration tests to verify they fail**

Run: `dotnet test tests/Todo.Api.IntegrationTests/Todo.Api.IntegrationTests.csproj --filter "FullyQualifiedName~SignUpEndpointsTests"`

Expected: FAIL because the endpoint still returns the old `"Duplicate user field."` title and uses ad-hoc status logic instead of the new shared mapping.

- [ ] **Step 3: Commit the red API contract tests if using red-green commits**

```bash
git add tests/Todo.Api.IntegrationTests/Features/Auth/SignUp/SignUpEndpointsTests.cs
git commit -m "test: define sign-up problem details contract"
```

### Task 4: Add a Shared Error-to-ProblemDetails Mapper

**Files:**
- Create: `src/Todo.Api/ExceptionHandling/ErrorProblemDetailsMapper.cs`
- Test: `tests/Todo.Api.IntegrationTests/Features/Auth/SignUp/SignUpEndpointsTests.cs`

- [ ] **Step 1: Create the reusable `ProblemDetails` mapper**

Create `src/Todo.Api/ExceptionHandling/ErrorProblemDetailsMapper.cs` so any endpoint can translate `Error` to base .NET `ProblemDetails` through one shared entry point.

```csharp
using BuildingBlocks.Application.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Todo.Api.ExceptionHandling;

public static class ErrorProblemDetailsMapper
{
    public static ProblemDetails Map(Error error)
    {
        var (status, title) = error.Type switch
        {
            ErrorType.Validation => (StatusCodes.Status400BadRequest, "Validation Failed"),
            ErrorType.Unauthorized => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            ErrorType.Forbidden => (StatusCodes.Status403Forbidden, "Forbidden"),
            ErrorType.NotFound => (StatusCodes.Status404NotFound, "Not Found"),
            ErrorType.Conflict => (StatusCodes.Status409Conflict, "Conflict"),
            ErrorType.Unexpected => (StatusCodes.Status500InternalServerError, "Unexpected Error"),
            _ => (StatusCodes.Status400BadRequest, "Request Failed")
        };

        return new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = error.Message,
            Type = error.Code
        };
    }
}
```

- [ ] **Step 2: Run the focused integration tests to verify the mapper file compiles once consumed**

Run: `dotnet test tests/Todo.Api.IntegrationTests/Todo.Api.IntegrationTests.csproj --filter "FullyQualifiedName~SignUpEndpointsTests"`

Expected: FAIL because `SignUp.Endpoint` still has the old inline logic and does not consume `ErrorProblemDetailsMapper` yet.

- [ ] **Step 3: Commit the shared mapper addition**

```bash
git add src/Todo.Api/ExceptionHandling/ErrorProblemDetailsMapper.cs
git commit -m "feat: add shared error problem details mapper"
```

### Task 5: Consume the Shared Mapper in the Sign-Up Endpoint

**Files:**
- Modify: `src/Todo.Api/Features/Auth/SignUp/Endpoint.cs`
- Test: `tests/Todo.Api.IntegrationTests/Features/Auth/SignUp/SignUpEndpointsTests.cs`

- [ ] **Step 1: Replace inline error comparisons with the shared mapper**

Update `src/Todo.Api/Features/Auth/SignUp/Endpoint.cs` so the endpoint delegates all `ProblemDetails` creation to `ErrorProblemDetailsMapper`.

```csharp
using BuildingBlocks.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Todo.Api.ExceptionHandling;

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

                var problemDetails = ErrorProblemDetailsMapper.Map(result.Error!);

                return Results.Problem(
                    title: problemDetails.Title,
                    type: problemDetails.Type,
                    detail: problemDetails.Detail,
                    statusCode: problemDetails.Status);
            })
            .WithName("AuthSignUp")
            .WithTags("Auth");
    }
}
```

- [ ] **Step 2: Run the focused integration tests to verify the endpoint contract now passes**

Run: `dotnet test tests/Todo.Api.IntegrationTests/Todo.Api.IntegrationTests.csproj --filter "FullyQualifiedName~SignUpEndpointsTests"`

Expected: PASS

- [ ] **Step 3: Run the related unit tests to catch regressions in the sign-up slice**

Run: `dotnet test tests/Todo.Api.UnitTests/Todo.Api.UnitTests.csproj --filter "FullyQualifiedName~SignUp"`

Expected: PASS

- [ ] **Step 4: Commit the endpoint mapping change**

```bash
git add src/Todo.Api/Features/Auth/SignUp/Endpoint.cs tests/Todo.Api.IntegrationTests/Features/Auth/SignUp/SignUpEndpointsTests.cs
git commit -m "refactor: map sign-up errors through error type"
```

### Task 6: Final Verification and Cleanup

**Files:**
- Verify only

- [ ] **Step 1: Run the full unit test project**

Run: `dotnet test tests/Todo.Api.UnitTests/Todo.Api.UnitTests.csproj`

Expected: PASS

- [ ] **Step 2: Run the full integration test project**

Run: `dotnet test tests/Todo.Api.IntegrationTests/Todo.Api.IntegrationTests.csproj`

Expected: PASS

- [ ] **Step 3: Inspect the final diff before handing off**

Run: `git diff -- src/BuildingBlocks/Application/Results/ErrorType.cs src/BuildingBlocks/Application/Results/Error.cs src/Todo.Api/Features/Auth/AuthErrors.cs src/Todo.Api/ExceptionHandling/ErrorProblemDetailsMapper.cs src/Todo.Api/Features/Auth/SignUp/Endpoint.cs tests/Todo.Api.UnitTests/Features/Auth/SignUp/SignUpHandlerTests.cs tests/Todo.Api.IntegrationTests/Features/Auth/SignUp/SignUpEndpointsTests.cs`

Expected: Shows only the new `ErrorType` metadata, auth error classification, shared `ProblemDetails` mapper, endpoint usage of that mapper, and the corresponding test updates.

- [ ] **Step 4: Commit the verification checkpoint if your workflow wants a final green marker**

```bash
git add docs/superpowers/plans/2026-05-30-error-type-problem-details-plan.md
git commit -m "docs: add error type mapping implementation plan"
```
