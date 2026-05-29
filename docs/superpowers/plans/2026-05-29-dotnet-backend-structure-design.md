# SuperPowerAI Backend Starter Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build `SuperPowerAI`, a production-ready .NET 10 backend starter with PostgreSQL, vertical slices, MediatR CQRS, FluentValidation, Scalar, Serilog, and xUnit-based test coverage, using a sample module as the reference pattern.

**Architecture:** The solution is a modular monolith with `AppHost.Api` as the composition root, `AppModules` for slice-first feature code, `BuildingBlocks` for shared application primitives, and `Infrastructure` for EF Core and PostgreSQL wiring. Minimal API endpoints live beside their request/handler code, a shared route group defines `/api/v1` once for all feature endpoints, and cross-cutting behavior is centralized through MediatR pipeline behaviors and host-level middleware.

**Tech Stack:** .NET 10, ASP.NET Core Minimal API, EF Core, Npgsql, MediatR, FluentValidation, Scalar.AspNetCore, Serilog.AspNetCore, xUnit, FluentAssertions, Microsoft.AspNetCore.Mvc.Testing, Testcontainers.PostgreSql

---

## File Structure

### Solution and Build Files

- `SuperPowerAI.sln`
  Responsibility: root solution file that includes all source and test projects.
- `Directory.Build.props`
  Responsibility: shared target framework, nullable, implicit usings, analyzers, and test-friendly warnings policy.
- `global.json`
  Responsibility: pin the .NET SDK used by local development and CI.
- `.editorconfig`
  Responsibility: repository-wide formatting and analyzer defaults.
- `.gitignore`
  Responsibility: ignore build outputs, local secrets, test artifacts, and IDE files.
- `docker-compose.yml`
  Responsibility: local PostgreSQL container for development.
- `README.md`
  Responsibility: developer entrypoint for restore, database startup, migrations, and running tests.

### Source Projects

- `src/AppHost.Api/AppHost.Api.csproj`
  Responsibility: API host, composition root, middleware, OpenAPI, Scalar, health checks, endpoint mapping.
- `src/AppHost.Api/Program.cs`
  Responsibility: host bootstrapping and request pipeline.
- `src/AppHost.Api/Extensions/ServiceCollectionExtensions.cs`
  Responsibility: host-level DI registration for modules, infrastructure, MediatR, validation, OpenAPI, and health checks.
- `src/AppHost.Api/Extensions/ApplicationExtensions.cs`
  Responsibility: middleware and endpoint mapping extension methods.
- `src/AppHost.Api/ExceptionHandling/GlobalExceptionHandler.cs`
  Responsibility: centralized exception-to-ProblemDetails mapping.

- `src/AppModules/AppModules.csproj`
  Responsibility: vertical slices and business-neutral sample module.
- `src/AppModules/Sample/Domain/SampleItem.cs`
  Responsibility: simple sample persistence entity used by the reference slices.
- `src/AppModules/Sample/Features/Create/Command.cs`
- `src/AppModules/Sample/Features/Create/Validator.cs`
- `src/AppModules/Sample/Features/Create/Handler.cs`
- `src/AppModules/Sample/Features/Create/Endpoint.cs`
- `src/AppModules/Sample/Features/Create/Response.cs`
  Responsibility: write-side sample slice.
- `src/AppModules/Sample/Features/GetById/Query.cs`
- `src/AppModules/Sample/Features/GetById/Handler.cs`
- `src/AppModules/Sample/Features/GetById/Endpoint.cs`
- `src/AppModules/Sample/Features/GetById/Response.cs`
  Responsibility: single-item read slice.
- `src/AppModules/Sample/Features/List/Query.cs`
- `src/AppModules/Sample/Features/List/Handler.cs`
- `src/AppModules/Sample/Features/List/Endpoint.cs`
- `src/AppModules/Sample/Features/List/Response.cs`
  Responsibility: list read slice.

- `src/BuildingBlocks/BuildingBlocks.csproj`
  Responsibility: shared contracts, error/result primitives, and MediatR pipeline behaviors.
- `src/BuildingBlocks/Abstractions/IEndpoint.cs`
  Responsibility: endpoint discovery contract for Minimal API slices.
- `src/BuildingBlocks/Abstractions/IEndpointModule.cs`
  Responsibility: optional marker for future module-based endpoint grouping.
- `src/BuildingBlocks/Endpoints/EndpointRegistrationExtensions.cs`
  Responsibility: reflection-based endpoint discovery and mapping into a shared API route group.
- `src/BuildingBlocks/Application/Behaviors/ValidationBehavior.cs`
  Responsibility: run FluentValidation validators before handlers.
- `src/BuildingBlocks/Application/Behaviors/RequestLoggingBehavior.cs`
  Responsibility: log request execution boundaries through MediatR.
- `src/BuildingBlocks/Application/Exceptions/RequestValidationException.cs`
  Responsibility: transport-friendly validation exception carrying field errors.
- `src/BuildingBlocks/Application/Results/Error.cs`
- `src/BuildingBlocks/Application/Results/Result.cs`
  Responsibility: explicit success/failure primitive for handlers, mapped to HTTP responses at the endpoint boundary.

- `src/Infrastructure/Infrastructure.csproj`
  Responsibility: EF Core, Npgsql, database configuration, migrations, and infrastructure DI.
- `src/Infrastructure/DependencyInjection.cs`
  Responsibility: infrastructure service registration.
- `src/Infrastructure/Persistence/ApplicationDbContext.cs`
  Responsibility: EF Core DbContext exposing sample entities.
- `src/Infrastructure/Persistence/Configurations/SampleItemConfiguration.cs`
  Responsibility: sample entity mapping.
- `src/Infrastructure/Persistence/Migrations/*`
  Responsibility: initial schema creation for PostgreSQL.
  Migration commands run with `src/AppHost.Api` as the startup project so EF uses the same host-based configuration as runtime.

### Test Projects

- `tests/AppModules.UnitTests/AppModules.UnitTests.csproj`
  Responsibility: handler and validator unit tests.
- `tests/AppModules.UnitTests/Sample/CreateSampleValidatorTests.cs`
- `tests/AppModules.UnitTests/Sample/CreateSampleHandlerTests.cs`
- `tests/AppModules.UnitTests/Sample/ListSamplesHandlerTests.cs`
  Responsibility: slice-level correctness.

- `tests/AppHost.Api.IntegrationTests/AppHost.Api.IntegrationTests.csproj`
  Responsibility: host bootstrapping and end-to-end API tests.
- `tests/AppHost.Api.IntegrationTests/Fixtures/PostgresContainerFixture.cs`
  Responsibility: PostgreSQL lifecycle for integration tests.
- `tests/AppHost.Api.IntegrationTests/Fixtures/CustomWebApplicationFactory.cs`
  Responsibility: test host bootstrapping with container connection string.
- `tests/AppHost.Api.IntegrationTests/HealthEndpointTests.cs`
- `tests/AppHost.Api.IntegrationTests/SampleEndpointsTests.cs`
  Responsibility: runtime verification of API and persistence wiring.

## Task 1: Scaffold the Solution Skeleton

**Files:**

- Create: `SuperPowerAI.sln`
- Create: `Directory.Build.props`
- Create: `global.json`
- Create: `.editorconfig`
- Create: `.gitignore`
- Create: `src/AppHost.Api/AppHost.Api.csproj`
- Create: `src/AppModules/AppModules.csproj`
- Create: `src/BuildingBlocks/BuildingBlocks.csproj`
- Create: `src/Infrastructure/Infrastructure.csproj`
- Create: `tests/AppModules.UnitTests/AppModules.UnitTests.csproj`
- Create: `tests/AppHost.Api.IntegrationTests/AppHost.Api.IntegrationTests.csproj`

- [x] **Step 1: Create the solution and projects**

```bash
dotnet new sln -n SuperPowerAI
dotnet new web -n AppHost.Api -o src/AppHost.Api
dotnet new classlib -n AppModules -o src/AppModules
dotnet new classlib -n BuildingBlocks -o src/BuildingBlocks
dotnet new classlib -n Infrastructure -o src/Infrastructure
dotnet new xunit -n AppModules.UnitTests -o tests/AppModules.UnitTests
dotnet new xunit -n AppHost.Api.IntegrationTests -o tests/AppHost.Api.IntegrationTests
dotnet sln SuperPowerAI.sln add src/AppHost.Api/AppHost.Api.csproj src/AppModules/AppModules.csproj src/BuildingBlocks/BuildingBlocks.csproj src/Infrastructure/Infrastructure.csproj tests/AppModules.UnitTests/AppModules.UnitTests.csproj tests/AppHost.Api.IntegrationTests/AppHost.Api.IntegrationTests.csproj
dotnet add src/AppHost.Api/AppHost.Api.csproj reference src/AppModules/AppModules.csproj src/BuildingBlocks/BuildingBlocks.csproj src/Infrastructure/Infrastructure.csproj
dotnet add src/AppModules/AppModules.csproj reference src/BuildingBlocks/BuildingBlocks.csproj
dotnet add src/Infrastructure/Infrastructure.csproj reference src/BuildingBlocks/BuildingBlocks.csproj src/AppModules/AppModules.csproj
dotnet add tests/AppModules.UnitTests/AppModules.UnitTests.csproj reference src/AppModules/AppModules.csproj src/BuildingBlocks/BuildingBlocks.csproj src/Infrastructure/Infrastructure.csproj
dotnet add tests/AppHost.Api.IntegrationTests/AppHost.Api.IntegrationTests.csproj reference src/AppHost.Api/AppHost.Api.csproj
```

- [x] **Step 2: Add shared build configuration**

```xml
<!-- Directory.Build.props -->
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
</Project>
```

```json
// global.json
{
    "sdk": {
        "version": "10.0.100",
        "rollForward": "latestFeature"
    }
}
```

```editorconfig
# .editorconfig
root = true

[*]
charset = utf-8
end_of_line = lf
insert_final_newline = true
indent_style = space
indent_size = 4

[*.cs]
dotnet_sort_system_directives_first = true
csharp_style_namespace_declarations = file_scoped:warning
```

```gitignore
# .gitignore
bin/
obj/
.vs/
.idea/
TestResults/
*.user
*.suo
```

- [x] **Step 3: Run a baseline build**

Run: `dotnet build SuperPowerAI.sln`
Expected: `Build succeeded.`

- [x] **Step 4: Commit**

```bash
git add SuperPowerAI.sln Directory.Build.props global.json .editorconfig .gitignore src tests
git commit -m "chore: scaffold backend starter solution"
```

## Task 2: Add Package References and Shared Application Primitives

**Files:**

- Modify: `src/AppHost.Api/AppHost.Api.csproj`
- Modify: `src/AppModules/AppModules.csproj`
- Modify: `src/BuildingBlocks/BuildingBlocks.csproj`
- Modify: `src/Infrastructure/Infrastructure.csproj`
- Modify: `tests/AppModules.UnitTests/AppModules.UnitTests.csproj`
- Modify: `tests/AppHost.Api.IntegrationTests/AppHost.Api.IntegrationTests.csproj`
- Create: `src/BuildingBlocks/Abstractions/IEndpoint.cs`
- Create: `src/BuildingBlocks/Abstractions/IEndpointModule.cs`
- Create: `src/BuildingBlocks/Endpoints/EndpointRegistrationExtensions.cs`
- Create: `src/BuildingBlocks/Application/Behaviors/ValidationBehavior.cs`
- Create: `src/BuildingBlocks/Application/Behaviors/RequestLoggingBehavior.cs`
- Create: `src/BuildingBlocks/Application/Exceptions/RequestValidationException.cs`
- Create: `src/BuildingBlocks/Application/Results/Error.cs`
- Create: `src/BuildingBlocks/Application/Results/Result.cs`

- [x] **Step 1: Add the required NuGet packages**

```bash
dotnet add src/AppHost.Api/AppHost.Api.csproj package Scalar.AspNetCore
dotnet add src/AppHost.Api/AppHost.Api.csproj package Serilog.AspNetCore
dotnet add src/AppHost.Api/AppHost.Api.csproj package MediatR
dotnet add src/AppHost.Api/AppHost.Api.csproj package FluentValidation.DependencyInjectionExtensions
dotnet add src/AppHost.Api/AppHost.Api.csproj package Microsoft.AspNetCore.OpenApi
dotnet add src/AppModules/AppModules.csproj package MediatR
dotnet add src/AppModules/AppModules.csproj package FluentValidation
dotnet add src/BuildingBlocks/BuildingBlocks.csproj package MediatR
dotnet add src/BuildingBlocks/BuildingBlocks.csproj package FluentValidation
dotnet add src/Infrastructure/Infrastructure.csproj package Microsoft.EntityFrameworkCore
dotnet add src/Infrastructure/Infrastructure.csproj package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add tests/AppModules.UnitTests/AppModules.UnitTests.csproj package FluentAssertions
dotnet add tests/AppModules.UnitTests/AppModules.UnitTests.csproj package NSubstitute
dotnet add tests/AppHost.Api.IntegrationTests/AppHost.Api.IntegrationTests.csproj package Microsoft.AspNetCore.Mvc.Testing
dotnet add tests/AppHost.Api.IntegrationTests/AppHost.Api.IntegrationTests.csproj package FluentAssertions
dotnet add tests/AppHost.Api.IntegrationTests/AppHost.Api.IntegrationTests.csproj package Testcontainers.PostgreSql
```

- [x] **Step 2: Define the endpoint contract and discovery extensions**

```csharp
// src/BuildingBlocks/Abstractions/IEndpoint.cs
namespace BuildingBlocks.Abstractions;

public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}
```

```csharp
// src/BuildingBlocks/Endpoints/EndpointRegistrationExtensions.cs
using System.Reflection;
using BuildingBlocks.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Endpoints;

public static class EndpointRegistrationExtensions
{
    public static IServiceCollection AddEndpoints(this IServiceCollection services, params Assembly[] assemblies)
    {
        var endpointTypes = assemblies
            .SelectMany(assembly => assembly.DefinedTypes)
            .Where(type => type is { IsAbstract: false, IsInterface: false } && typeof(IEndpoint).IsAssignableFrom(type))
            .ToArray();

        foreach (var endpointType in endpointTypes)
        {
            services.AddTransient(typeof(IEndpoint), endpointType);
        }

        return services;
    }

    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder app)
    {
        var endpoints = app.ServiceProvider.GetRequiredService<IEnumerable<IEndpoint>>();

        foreach (var endpoint in endpoints)
        {
            endpoint.MapEndpoint(app);
        }

        return app;
    }
}
```

- [x] **Step 3: Define shared result and behavior primitives**

```csharp
// src/BuildingBlocks/Application/Results/Result.cs
namespace BuildingBlocks.Application.Results;

public class Result
{
    protected Result(bool isSuccess, Error? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error? Error { get; }

    public static Result Success() => new(true, null);
    public static Result Failure(Error error) => new(false, error);
}

public sealed class Result<T> : Result
{
    private Result(T? value, bool isSuccess, Error? error) : base(isSuccess, error)
    {
        Value = value;
    }

    public T? Value { get; }

    public static Result<T> Success(T value) => new(value, true, null);
    public static new Result<T> Failure(Error error) => new(default, false, error);
}
```

```csharp
// src/BuildingBlocks/Application/Behaviors/ValidationBehavior.cs
using FluentValidation;
using MediatR;

namespace BuildingBlocks.Application.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);
        var failures = await Task.WhenAll(validators.Select(v => v.ValidateAsync(context, cancellationToken)));
        var errors = failures
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .GroupBy(failure => failure.PropertyName)
            .ToDictionary(group => group.Key, group => group.Select(x => x.ErrorMessage).ToArray());

        if (errors.Count != 0)
        {
            throw new RequestValidationException(errors);
        }

        return await next();
    }
}
```

```csharp
// src/BuildingBlocks/Application/Exceptions/RequestValidationException.cs
namespace BuildingBlocks.Application.Exceptions;

public sealed class RequestValidationException(Dictionary<string, string[]> errors) : Exception("Validation failed.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
```

```csharp
// src/BuildingBlocks/Application/Behaviors/RequestLoggingBehavior.cs
using MediatR;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Application.Behaviors;

public sealed class RequestLoggingBehavior<TRequest, TResponse>(ILogger<RequestLoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling request {RequestName} {@Request}", typeof(TRequest).Name, request);
        var response = await next();
        logger.LogInformation("Handled request {RequestName}", typeof(TRequest).Name);
        return response;
    }
}
```

```csharp
// src/BuildingBlocks/Application/Results/Error.cs
namespace BuildingBlocks.Application.Results;

public sealed record Error(string Code, string Message)
{
    public static readonly Error NotFound = new("common.not_found", "The requested resource was not found.");
    public static readonly Error Validation = new("common.validation", "The request failed validation.");
}
```

- [x] **Step 4: Build to catch package and primitive errors**

Run: `dotnet build SuperPowerAI.sln`
Expected: `Build succeeded.`

- [x] **Step 5: Commit**

```bash
git add src tests
git commit -m "feat: add shared backend primitives"
```

## Task 3: Add Infrastructure and Persistence Boundaries

**Files:**

- Create: `src/AppModules/Sample/Domain/SampleItem.cs`
- Create: `src/Infrastructure/DependencyInjection.cs`
- Create: `src/Infrastructure/Persistence/ApplicationDbContext.cs`
- Create: `src/Infrastructure/Persistence/Configurations/SampleItemConfiguration.cs`
- Modify: `src/Infrastructure/Infrastructure.csproj`
- Create: `src/AppHost.Api/appsettings.json`
- Create: `src/AppHost.Api/appsettings.Development.json`
- Create: `docker-compose.yml`

- [x] **Step 1: Create the persistence model and DbContext**

```csharp
// src/AppModules/Sample/Domain/SampleItem.cs
namespace AppModules.Sample.Domain;

public sealed class SampleItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
}
```

```csharp
// src/Infrastructure/Persistence/ApplicationDbContext.cs
using AppModules.Sample.Domain;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<SampleItem> SampleItems => Set<SampleItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
```

```csharp
// src/Infrastructure/Persistence/Configurations/SampleItemConfiguration.cs
using AppModules.Sample.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class SampleItemConfiguration : IEntityTypeConfiguration<SampleItem>
{
    public void Configure(EntityTypeBuilder<SampleItem> builder)
    {
        builder.ToTable("sample_items");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.CreatedUtc).IsRequired();
    }
}
```

- [x] **Step 2: Configure PostgreSQL-backed infrastructure registration**

```csharp
// src/Infrastructure/DependencyInjection.cs
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Connection string 'Postgres' was not found.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        return services;
    }
}
```

```yaml
# docker-compose.yml
services:
    postgres:
        image: postgres:17
        container_name: superpowerai-postgres
        environment:
            POSTGRES_DB: superpowerai
            POSTGRES_USER: postgres
            POSTGRES_PASSWORD: postgres
        ports:
            - "5433:5432"
        volumes:
            - postgres-data:/var/lib/postgresql/data

volumes:
    postgres-data:
```

- [x] **Step 3: Add environment-based configuration**

```json
// src/AppHost.Api/appsettings.json
{
    "ConnectionStrings": {
        "Postgres": "Host=localhost;Port=5433;Database=superpowerai;Username=postgres;Password=postgres"
    },
    "Serilog": {
        "Using": ["Serilog.Sinks.Console"],
        "MinimumLevel": {
            "Default": "Information",
            "Override": {
                "Microsoft": "Warning",
                "Microsoft.AspNetCore": "Warning"
            }
        },
        "WriteTo": [{ "Name": "Console" }],
        "Enrich": ["FromLogContext"]
    }
}
```

```json
// src/AppHost.Api/appsettings.Development.json
{
    "Logging": {
        "LogLevel": {
            "Default": "Information",
            "Microsoft.AspNetCore": "Warning"
        }
    }
}
```

- [x] **Step 4: Add the first migration and verify infrastructure builds**

Run: `dotnet ef migrations add InitialCreate --project src/Infrastructure --startup-project src/AppHost.Api --output-dir Persistence/Migrations`
Expected: migration files appear under `src/Infrastructure/Persistence/Migrations`

Run: `dotnet build SuperPowerAI.sln`
Expected: `Build succeeded.`

- [x] **Step 5: Commit**

```bash
git add src/AppModules src/Infrastructure src/AppHost.Api docker-compose.yml
git commit -m "feat: add database infrastructure"
```

## Task 4: Implement the Sample Vertical Slices

**Files:**

- Create: `src/AppModules/Sample/Features/Create/Command.cs`
- Create: `src/AppModules/Sample/Features/Create/Validator.cs`
- Create: `src/AppModules/Sample/Features/Create/Handler.cs`
- Create: `src/AppModules/Sample/Features/Create/Endpoint.cs`
- Create: `src/AppModules/Sample/Features/Create/Response.cs`
- Create: `src/AppModules/Sample/Features/GetById/Query.cs`
- Create: `src/AppModules/Sample/Features/GetById/Handler.cs`
- Create: `src/AppModules/Sample/Features/GetById/Endpoint.cs`
- Create: `src/AppModules/Sample/Features/GetById/Response.cs`
- Create: `src/AppModules/Sample/Features/List/Query.cs`
- Create: `src/AppModules/Sample/Features/List/Handler.cs`
- Create: `src/AppModules/Sample/Features/List/Endpoint.cs`
- Create: `src/AppModules/Sample/Features/List/Response.cs`

- [x] **Step 1: Write the first failing validator test for create**

```csharp
// tests/AppModules.UnitTests/Sample/CreateSampleValidatorTests.cs
using AppModules.Sample.Features.Create;
using FluentAssertions;

namespace AppModules.UnitTests.Sample;

public sealed class CreateSampleValidatorTests
{
    [Fact]
    public void Validate_Should_Fail_When_Name_Is_Empty()
    {
        var validator = new Validator();
        var command = new Command(string.Empty);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.PropertyName == nameof(Command.Name));
    }
}
```

- [x] **Step 2: Run the unit test to verify it fails**

Run: `dotnet test tests/AppModules.UnitTests/AppModules.UnitTests.csproj --filter CreateSampleValidatorTests -v minimal`
Expected: FAIL because `Command` and `Validator` do not exist yet

- [x] **Step 3: Implement the create slice**

```csharp
// src/AppModules/Sample/Features/Create/Command.cs
using BuildingBlocks.Application.Results;
using MediatR;

namespace AppModules.Sample.Features.Create;

public sealed record Command(string Name) : IRequest<Result<Response>>;
```

```csharp
// src/AppModules/Sample/Features/Create/Validator.cs
using FluentValidation;

namespace AppModules.Sample.Features.Create;

public sealed class Validator : AbstractValidator<Command>
{
    public Validator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
```

```csharp
// src/AppModules/Sample/Features/Create/Handler.cs
using AppModules.Sample.Domain;
using BuildingBlocks.Application.Results;
using Infrastructure.Persistence;
using MediatR;

namespace AppModules.Sample.Features.Create;

public sealed class Handler(ApplicationDbContext dbContext) : IRequestHandler<Command, Result<Response>>
{
    public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
    {
        var entity = new SampleItem
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            CreatedUtc = DateTime.UtcNow
        };

        dbContext.SampleItems.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<Response>.Success(new Response(entity.Id, entity.Name, entity.CreatedUtc));
    }
}
```

```csharp
// src/AppModules/Sample/Features/Create/Response.cs
namespace AppModules.Sample.Features.Create;

public sealed record Response(Guid Id, string Name, DateTime CreatedUtc);
```

```csharp
// src/AppModules/Sample/Features/Create/Endpoint.cs
using BuildingBlocks.Abstractions;
using Microsoft.AspNetCore.Mvc;
using MediatR;

namespace AppModules.Sample.Features.Create;

public sealed class Endpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/sample", async (Command command, ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(command, cancellationToken);
                return result.IsSuccess
                    ? Results.CreatedAtRoute("GetSampleById", new { id = result.Value!.Id }, result.Value)
                    : Results.BadRequest(new ProblemDetails
                    {
                        Title = "Request failed",
                        Detail = result.Error!.Message,
                        Type = result.Error.Code,
                        Status = StatusCodes.Status400BadRequest
                    });
            })
            .WithName("CreateSample")
            .WithTags("Sample")
            .WithOpenApi();
    }
}
```

- [x] **Step 4: Implement the read slices**

```csharp
// src/AppModules/Sample/Features/GetById/Query.cs
using BuildingBlocks.Application.Results;
using MediatR;

namespace AppModules.Sample.Features.GetById;

public sealed record Query(Guid Id) : IRequest<Result<Response>>;
```

```csharp
// src/AppModules/Sample/Features/GetById/Handler.cs
using BuildingBlocks.Application.Results;
using Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AppModules.Sample.Features.GetById;

public sealed class Handler(ApplicationDbContext dbContext) : IRequestHandler<Query, Result<Response>>
{
    public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
    {
        var item = await dbContext.SampleItems
            .AsNoTracking()
            .Where(x => x.Id == request.Id)
            .Select(x => new Response(x.Id, x.Name, x.CreatedUtc))
            .SingleOrDefaultAsync(cancellationToken);

        return item is null
            ? Result<Response>.Failure(Error.NotFound)
            : Result<Response>.Success(item);
    }
}
```

```csharp
// src/AppModules/Sample/Features/GetById/Endpoint.cs
using BuildingBlocks.Abstractions;
using Microsoft.AspNetCore.Mvc;
using MediatR;

namespace AppModules.Sample.Features.GetById;

public sealed class Endpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/sample/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new Query(id), cancellationToken);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.NotFound(new ProblemDetails
                    {
                        Title = "Resource not found",
                        Detail = result.Error!.Message,
                        Type = result.Error.Code,
                        Status = StatusCodes.Status404NotFound
                    });
            })
            .WithName("GetSampleById")
            .WithTags("Sample")
            .WithOpenApi();
    }
}
```

```csharp
// src/AppModules/Sample/Features/GetById/Response.cs
namespace AppModules.Sample.Features.GetById;

public sealed record Response(Guid Id, string Name, DateTime CreatedUtc);
```

```csharp
// src/AppModules/Sample/Features/List/Query.cs
using BuildingBlocks.Application.Results;
using MediatR;

namespace AppModules.Sample.Features.List;

public sealed record Query() : IRequest<Result<IReadOnlyList<Response>>>;
```

```csharp
// src/AppModules/Sample/Features/List/Handler.cs
using BuildingBlocks.Application.Results;
using Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AppModules.Sample.Features.List;

public sealed class Handler(ApplicationDbContext dbContext) : IRequestHandler<Query, Result<IReadOnlyList<Response>>>
{
    public async Task<Result<IReadOnlyList<Response>>> Handle(Query request, CancellationToken cancellationToken)
    {
        var items = await dbContext.SampleItems
            .AsNoTracking()
            .OrderBy(x => x.CreatedUtc)
            .Select(x => new Response(x.Id, x.Name, x.CreatedUtc))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<Response>>.Success(items);
    }
}
```

```csharp
// src/AppModules/Sample/Features/List/Endpoint.cs
using BuildingBlocks.Abstractions;
using MediatR;

namespace AppModules.Sample.Features.List;

public sealed class Endpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/sample", async (ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new Query(), cancellationToken);
                return Results.Ok(result.Value);
            })
            .WithName("ListSamples")
            .WithTags("Sample")
            .WithOpenApi();
    }
}
```

```csharp
// src/AppModules/Sample/Features/List/Response.cs
namespace AppModules.Sample.Features.List;

public sealed record Response(Guid Id, string Name, DateTime CreatedUtc);
```

- [x] **Step 5: Run unit tests to verify the create validator now passes**

Run: `dotnet test tests/AppModules.UnitTests/AppModules.UnitTests.csproj --filter CreateSampleValidatorTests -v minimal`
Expected: PASS

- [x] **Step 6: Commit**

```bash
git add src/AppModules tests/AppModules.UnitTests
git commit -m "feat: add sample vertical slices"
```

## Task 5: Wire the API Host and Cross-Cutting Behavior

**Files:**

- Create: `src/AppHost.Api/Extensions/ServiceCollectionExtensions.cs`
- Create: `src/AppHost.Api/Extensions/ApplicationExtensions.cs`
- Create: `src/AppHost.Api/ExceptionHandling/GlobalExceptionHandler.cs`
- Modify: `src/AppHost.Api/Program.cs`
- Modify: `src/BuildingBlocks/Application/Behaviors/RequestLoggingBehavior.cs`
- Modify: `src/BuildingBlocks/Application/Exceptions/RequestValidationException.cs`

- [x] **Step 1: Write the first failing integration test for health bootstrapping**

```csharp
// tests/AppHost.Api.IntegrationTests/HealthEndpointTests.cs
using System.Net;
using FluentAssertions;

namespace AppHost.Api.IntegrationTests;

public sealed class HealthEndpointTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Get_Health_Should_Return_Ok()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

- [x] **Step 2: Run the health test to verify it fails**

Run: `dotnet test tests/AppHost.Api.IntegrationTests/AppHost.Api.IntegrationTests.csproj --filter HealthEndpointTests -v minimal`
Expected: FAIL because the host extensions and test factory do not exist yet

- [x] **Step 3: Implement the host composition root**

```csharp
// src/AppHost.Api/Program.cs
using AppHost.Api.Extensions;
using AppHost.Api.ExceptionHandling;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddApplicationServices(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseSerilogRequestLogging();
app.UseApplicationPipeline();

var apiV1 = app.MapGroup("/api/v1");
apiV1.MapEndpoints();
app.MapHealthChecks("/health");
app.MapOpenApi();

if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference();
}

app.Run();

public partial class Program;
```

```csharp
// src/AppHost.Api/Extensions/ServiceCollectionExtensions.cs
using System.Reflection;
using BuildingBlocks.Application.Behaviors;
using BuildingBlocks.Endpoints;
using FluentValidation;
using Infrastructure;
using MediatR;

namespace AppHost.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOpenApi();
        services.AddHealthChecks();
        services.AddEndpoints(Assembly.Load("AppModules"));
        services.AddInfrastructure(configuration);
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.Load("AppModules"));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(RequestLoggingBehavior<,>));
        });
        services.AddValidatorsFromAssembly(Assembly.Load("AppModules"));

        return services;
    }
}
```

```csharp
// src/AppHost.Api/Extensions/ApplicationExtensions.cs
using BuildingBlocks.Endpoints;

namespace AppHost.Api.Extensions;

public static class ApplicationExtensions
{
    public static WebApplication UseApplicationPipeline(this WebApplication app)
    {
        app.UseHttpsRedirection();
        return app;
    }
}
```

- [x] **Step 4: Implement exception handling and request logging**

```csharp
// src/AppHost.Api/ExceptionHandling/GlobalExceptionHandler.cs
using BuildingBlocks.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace AppHost.Api.ExceptionHandling;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is RequestValidationException validationException)
        {
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await httpContext.Response.WriteAsJsonAsync(
                new ValidationProblemDetails(validationException.Errors)
                {
                    Title = "One or more validation errors occurred.",
                    Status = StatusCodes.Status400BadRequest
                },
                cancellationToken);
            return true;
        }

        logger.LogError(exception, "Unhandled exception");

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Title = "An unexpected error occurred.",
                Status = StatusCodes.Status500InternalServerError
            },
            cancellationToken);
        return true;
    }
}
```

- [x] **Step 5: Run the health integration test again**

Run: `dotnet test tests/AppHost.Api.IntegrationTests/AppHost.Api.IntegrationTests.csproj --filter HealthEndpointTests -v minimal`
Expected: PASS

- [x] **Step 6: Commit**

```bash
git add src/AppHost.Api src/BuildingBlocks tests/AppHost.Api.IntegrationTests
git commit -m "feat: wire api host and middleware"
```

## Task 6: Add Unit Tests for Validators and Handlers

**Files:**

- Create: `tests/AppModules.UnitTests/Sample/CreateSampleHandlerTests.cs`
- Create: `tests/AppModules.UnitTests/Sample/ListSamplesHandlerTests.cs`
- Modify: `tests/AppModules.UnitTests/Sample/CreateSampleValidatorTests.cs`

- [x] **Step 1: Write the failing create handler test**

```csharp
// tests/AppModules.UnitTests/Sample/CreateSampleHandlerTests.cs
using AppModules.Sample.Features.Create;
using FluentAssertions;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppModules.UnitTests.Sample;

public sealed class CreateSampleHandlerTests
{
    [Fact]
    public async Task Handle_Should_Persist_Entity_And_Return_Response()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new ApplicationDbContext(options);
        var handler = new Handler(dbContext);

        var result = await handler.Handle(new Command("First sample"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        dbContext.SampleItems.Should().ContainSingle(x => x.Name == "First sample");
    }
}
```

- [x] **Step 2: Run the test to verify it fails**

Run: `dotnet test tests/AppModules.UnitTests/AppModules.UnitTests.csproj --filter CreateSampleHandlerTests -v minimal`
Expected: FAIL until the project references and EF Core test usage are wired correctly

- [x] **Step 3: Add the remaining slice tests**

```csharp
// tests/AppModules.UnitTests/Sample/ListSamplesHandlerTests.cs
using AppModules.Sample.Domain;
using AppModules.Sample.Features.List;
using FluentAssertions;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppModules.UnitTests.Sample;

public sealed class ListSamplesHandlerTests
{
    [Fact]
    public async Task Handle_Should_Return_Items_Ordered_By_Creation_Time()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new ApplicationDbContext(options);
        dbContext.SampleItems.AddRange(
            new SampleItem { Id = Guid.NewGuid(), Name = "older", CreatedUtc = DateTime.UtcNow.AddMinutes(-2) },
            new SampleItem { Id = Guid.NewGuid(), Name = "newer", CreatedUtc = DateTime.UtcNow.AddMinutes(-1) });
        await dbContext.SaveChangesAsync();

        var handler = new Handler(dbContext);
        var result = await handler.Handle(new Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Select(x => x.Name).Should().ContainInOrder("older", "newer");
    }
}
```

- [x] **Step 4: Run the unit test suite**

Run: `dotnet test tests/AppModules.UnitTests/AppModules.UnitTests.csproj -v minimal`
Expected: PASS

- [x] **Step 5: Commit**

```bash
git add tests/AppModules.UnitTests
git commit -m "test: add slice unit coverage"
```

## Task 7: Add PostgreSQL-Backed Integration Tests

**Files:**

- Create: `tests/AppHost.Api.IntegrationTests/Fixtures/PostgresContainerFixture.cs`
- Create: `tests/AppHost.Api.IntegrationTests/Fixtures/CustomWebApplicationFactory.cs`
- Create: `tests/AppHost.Api.IntegrationTests/SampleEndpointsTests.cs`

- [ ] **Step 1: Write the first failing end-to-end sample flow test**

```csharp
// tests/AppHost.Api.IntegrationTests/SampleEndpointsTests.cs
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace AppHost.Api.IntegrationTests;

public sealed class SampleEndpointsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Post_Then_Get_Should_Roundtrip_Sample_Item()
    {
        var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/v1/sample", new { name = "roundtrip" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<CreateSampleResponse>();
        var getResponse = await client.GetAsync($"/api/v1/sample/{created!.Id}");

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private sealed record CreateSampleResponse(Guid Id, string Name, DateTime CreatedUtc);
}
```

- [ ] **Step 2: Run the integration test to verify it fails**

Run: `dotnet test tests/AppHost.Api.IntegrationTests/AppHost.Api.IntegrationTests.csproj --filter SampleEndpointsTests -v minimal`
Expected: FAIL because the PostgreSQL container fixture and database initialization are not implemented yet

- [ ] **Step 3: Implement the test container and host factory**

```csharp
// tests/AppHost.Api.IntegrationTests/Fixtures/PostgresContainerFixture.cs
using Testcontainers.PostgreSql;

namespace AppHost.Api.IntegrationTests.Fixtures;

public sealed class PostgresContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:17")
        .WithDatabase("superpowerai_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
```

```csharp
// tests/AppHost.Api.IntegrationTests/Fixtures/CustomWebApplicationFactory.cs
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AppHost.Api.IntegrationTests.Fixtures;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgresContainerFixture _postgres = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(_postgres.ConnectionString));

            using var scope = services.BuildServiceProvider().CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.Database.Migrate();
        });
    }

    public async Task InitializeAsync()
    {
        await _postgres.InitializeAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }
}
```

- [ ] **Step 4: Run the full integration suite**

Run: `dotnet test tests/AppHost.Api.IntegrationTests/AppHost.Api.IntegrationTests.csproj -v minimal`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add tests/AppHost.Api.IntegrationTests
git commit -m "test: add postgres-backed integration tests"
```

## Task 8: Finish Developer Operations and Final Verification

**Files:**

- Create: `README.md`
- Modify: `src/AppHost.Api/Program.cs`
- Modify: `src/AppHost.Api/appsettings.Development.json`
- Modify: `.gitignore`

- [ ] **Step 1: Add final developer workflow documentation**

````md
<!-- README.md -->

# SuperPowerAI

## Prerequisites

- .NET 10 SDK
- Docker Desktop

## Local database

```bash
docker compose up -d postgres
```
````

## Run the API

```bash
dotnet run --project src/AppHost.Api
```

Open Scalar at `http://localhost:5000/scalar/v1`.

## Tests

```bash
dotnet test SuperPowerAI.sln
```

````

- [ ] **Step 2: Verify the full starter end to end**

Run: `docker compose up -d postgres`
Expected: PostgreSQL container starts successfully

Run: `dotnet test SuperPowerAI.sln`
Expected: all unit and integration tests PASS

Run: `dotnet run --project src/AppHost.Api`
Expected: host starts, OpenAPI is exposed, `/health` returns `200 OK`, and Scalar loads in development

- [ ] **Step 3: Commit**

```bash
git add README.md src/AppHost.Api .gitignore
git commit -m "docs: finish backend starter setup"
````

## Self-Review

- Spec coverage: the plan covers the modular monolith layout, Minimal API host, shared `/api/v1` route-group registration, MediatR CQRS flow, FluentValidation behavior, Serilog logging, PostgreSQL persistence, Scalar/OpenAPI, health checks, Docker Compose, xUnit-based unit tests, and PostgreSQL-backed integration tests. It also standardizes plain data for successful responses and `ProblemDetails` for failures. Non-goals such as auth, caching, and message infrastructure are intentionally absent.
- Placeholder scan: no `TODO`, `TBD`, or “implement later” markers remain. Each task names exact files, commands, and expected outputs.
- Type consistency: the plan uses `SuperPowerAI`, `ApplicationDbContext`, `Command`, `Query`, `Response`, `RequestValidationException`, `ProblemDetails`, and the shared `/api/v1` route group consistently across tasks.
