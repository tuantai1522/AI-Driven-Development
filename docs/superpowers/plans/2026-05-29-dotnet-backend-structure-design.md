# SuperPowerAI Backend Starter Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build `SuperPowerAI`, a production-ready .NET 10 backend starter with PostgreSQL, vertical slices, MediatR CQRS, FluentValidation, Scalar, Serilog, and xUnit-based test coverage, using a sample feature as the reference pattern.

**Architecture:** The implementation is a modular monolith with `Todo.Api` as the single runtime project. `Todo.Api` owns the host, feature slices, persistence, DI, and EF Core migrations, while `BuildingBlocks` holds shared primitives. Tests mirror the runtime structure under `tests`.

**Tech Stack:** .NET 10, ASP.NET Core Minimal API, EF Core, Npgsql, MediatR, FluentValidation, Scalar.AspNetCore, Serilog.AspNetCore, xUnit, FluentAssertions, NSubstitute, Microsoft.AspNetCore.Mvc.Testing, Testcontainers.PostgreSql

---

## File Structure

### Solution and Build Files

- `SuperPowerAI.sln`
  Responsibility: root solution file that includes all runtime and test projects.
- `Directory.Build.props`
  Responsibility: shared target framework, nullable, implicit usings, and warning policy.
- `global.json`
  Responsibility: pin the .NET SDK used by local development and CI.
- `.editorconfig`
  Responsibility: repository-wide formatting and analyzer defaults.
- `.gitignore`
  Responsibility: ignore build outputs, local secrets, and IDE files.
- `docker-compose.yml`
  Responsibility: local PostgreSQL container for development.

### Source Projects

- `src/Todo.Api/Todo.Api.csproj`
  Responsibility: API host and main runtime assembly.
- `src/Todo.Api/Program.cs`
  Responsibility: host bootstrapping and request pipeline.
- `src/Todo.Api/Extensions/ServiceCollectionExtensions.cs`
  Responsibility: DI registration for endpoints, MediatR, validators, DbContext, and supporting services.
- `src/Todo.Api/Extensions/ApplicationExtensions.cs`
  Responsibility: middleware pipeline wiring.
- `src/Todo.Api/ExceptionHandling/GlobalExceptionHandler.cs`
  Responsibility: centralized exception-to-ProblemDetails mapping.
- `src/Todo.Api/Abstractions/Persistence/IApplicationDbContext.cs`
  Responsibility: database abstraction consumed by feature handlers.
- `src/Todo.Api/Persistence/ApplicationDbContext.cs`
  Responsibility: EF Core DbContext exposing persisted entities.
- `src/Todo.Api/Persistence/Models/SampleItem.cs`
  Responsibility: sample persisted entity.
- `src/Todo.Api/Persistence/Configurations/SampleItemConfiguration.cs`
  Responsibility: EF Core mapping for the sample entity.
- `src/Todo.Api/Persistence/Migrations/*`
  Responsibility: PostgreSQL schema history for the API.
- `src/Todo.Api/Features/Sample/Create/*`
  Responsibility: sample create slice.
- `src/Todo.Api/Features/Sample/GetById/*`
  Responsibility: sample single-item query slice.
- `src/Todo.Api/Features/Sample/List/*`
  Responsibility: sample list query slice.

- `src/BuildingBlocks/BuildingBlocks.csproj`
  Responsibility: shared contracts, error/result primitives, and MediatR pipeline behaviors.
- `src/BuildingBlocks/Abstractions/IEndpoint.cs`
  Responsibility: endpoint discovery contract for Minimal API slices.
- `src/BuildingBlocks/Abstractions/IEndpointModule.cs`
  Responsibility: optional marker for future module grouping.
- `src/BuildingBlocks/Endpoints/EndpointRegistrationExtensions.cs`
  Responsibility: reflection-based endpoint discovery and mapping.
- `src/BuildingBlocks/Application/Behaviors/ValidationBehavior.cs`
  Responsibility: run FluentValidation validators before handlers.
- `src/BuildingBlocks/Application/Behaviors/RequestLoggingBehavior.cs`
  Responsibility: log request execution boundaries through MediatR.
- `src/BuildingBlocks/Application/Exceptions/RequestValidationException.cs`
  Responsibility: transport-friendly validation exception carrying field errors.
- `src/BuildingBlocks/Application/Results/Error.cs`
- `src/BuildingBlocks/Application/Results/Result.cs`
  Responsibility: explicit success/failure primitive for handlers.

### Test Projects

- `tests/Todo.Api.UnitTests/Todo.Api.UnitTests.csproj`
  Responsibility: handler and validator unit tests.
- `tests/Todo.Api.UnitTests/Features/Sample/Create/CreateSampleValidatorTests.cs`
- `tests/Todo.Api.UnitTests/Features/Sample/Create/CreateSampleHandlerTests.cs`
- `tests/Todo.Api.UnitTests/Features/Sample/List/ListSamplesHandlerTests.cs`
  Responsibility: slice-level correctness.

- `tests/Todo.Api.IntegrationTests/Todo.Api.IntegrationTests.csproj`
  Responsibility: end-to-end API and PostgreSQL-backed integration tests.
- `tests/Todo.Api.IntegrationTests/Fixtures/PostgresContainerFixture.cs`
  Responsibility: PostgreSQL lifecycle for integration tests.
- `tests/Todo.Api.IntegrationTests/Fixtures/CustomWebApplicationFactory.cs`
  Responsibility: test host bootstrapping with container connection string.
- `tests/Todo.Api.IntegrationTests/System/HealthEndpointTests.cs`
- `tests/Todo.Api.IntegrationTests/Features/Sample/SampleEndpointsTests.cs`
  Responsibility: runtime verification of API and persistence wiring.

## Task 1: Scaffold the Solution Skeleton

**Files:**

- Create: `SuperPowerAI.sln`
- Create: `Directory.Build.props`
- Create: `global.json`
- Create: `.editorconfig`
- Create: `.gitignore`
- Create: `src/Todo.Api/Todo.Api.csproj`
- Create: `src/BuildingBlocks/BuildingBlocks.csproj`
- Create: `tests/Todo.Api.UnitTests/Todo.Api.UnitTests.csproj`
- Create: `tests/Todo.Api.IntegrationTests/Todo.Api.IntegrationTests.csproj`

- [x] **Step 1: Create the solution and projects**

```bash
dotnet new sln -n SuperPowerAI
dotnet new web -n Todo.Api -o src/Todo.Api
dotnet new classlib -n BuildingBlocks -o src/BuildingBlocks
dotnet new xunit -n Todo.Api.UnitTests -o tests/Todo.Api.UnitTests
dotnet new xunit -n Todo.Api.IntegrationTests -o tests/Todo.Api.IntegrationTests
dotnet sln SuperPowerAI.sln add src/Todo.Api/Todo.Api.csproj src/BuildingBlocks/BuildingBlocks.csproj tests/Todo.Api.UnitTests/Todo.Api.UnitTests.csproj tests/Todo.Api.IntegrationTests/Todo.Api.IntegrationTests.csproj
dotnet add src/Todo.Api/Todo.Api.csproj reference src/BuildingBlocks/BuildingBlocks.csproj
dotnet add tests/Todo.Api.UnitTests/Todo.Api.UnitTests.csproj reference src/Todo.Api/Todo.Api.csproj src/BuildingBlocks/BuildingBlocks.csproj
dotnet add tests/Todo.Api.IntegrationTests/Todo.Api.IntegrationTests.csproj reference src/Todo.Api/Todo.Api.csproj
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

- [x] **Step 3: Run a baseline build**

Run: `dotnet build SuperPowerAI.sln`
Expected: `Build succeeded.`

## Task 2: Add Package References and Shared Application Primitives

**Files:**

- Modify: `src/Todo.Api/Todo.Api.csproj`
- Modify: `src/BuildingBlocks/BuildingBlocks.csproj`
- Modify: `tests/Todo.Api.UnitTests/Todo.Api.UnitTests.csproj`
- Modify: `tests/Todo.Api.IntegrationTests/Todo.Api.IntegrationTests.csproj`
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
dotnet add src/Todo.Api/Todo.Api.csproj package Scalar.AspNetCore
dotnet add src/Todo.Api/Todo.Api.csproj package Serilog.AspNetCore
dotnet add src/Todo.Api/Todo.Api.csproj package MediatR
dotnet add src/Todo.Api/Todo.Api.csproj package FluentValidation
dotnet add src/Todo.Api/Todo.Api.csproj package FluentValidation.DependencyInjectionExtensions
dotnet add src/Todo.Api/Todo.Api.csproj package Microsoft.AspNetCore.OpenApi
dotnet add src/Todo.Api/Todo.Api.csproj package Microsoft.EntityFrameworkCore
dotnet add src/Todo.Api/Todo.Api.csproj package Microsoft.EntityFrameworkCore.Design
dotnet add src/Todo.Api/Todo.Api.csproj package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add src/BuildingBlocks/BuildingBlocks.csproj package MediatR
dotnet add src/BuildingBlocks/BuildingBlocks.csproj package FluentValidation
dotnet add tests/Todo.Api.UnitTests/Todo.Api.UnitTests.csproj package FluentAssertions
dotnet add tests/Todo.Api.UnitTests/Todo.Api.UnitTests.csproj package NSubstitute
dotnet add tests/Todo.Api.UnitTests/Todo.Api.UnitTests.csproj package Microsoft.EntityFrameworkCore.InMemory
dotnet add tests/Todo.Api.IntegrationTests/Todo.Api.IntegrationTests.csproj package Microsoft.AspNetCore.Mvc.Testing
dotnet add tests/Todo.Api.IntegrationTests/Todo.Api.IntegrationTests.csproj package FluentAssertions
dotnet add tests/Todo.Api.IntegrationTests/Todo.Api.IntegrationTests.csproj package Testcontainers.PostgreSql
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

- [x] **Step 3: Build to catch package and primitive errors**

Run: `dotnet build SuperPowerAI.sln`
Expected: `Build succeeded.`

## Task 3: Add Persistence Boundaries Inside `Todo.Api`

**Files:**

- Create: `src/Todo.Api/Abstractions/Persistence/IApplicationDbContext.cs`
- Create: `src/Todo.Api/Persistence/ApplicationDbContext.cs`
- Create: `src/Todo.Api/Persistence/Models/SampleItem.cs`
- Create: `src/Todo.Api/Persistence/Configurations/SampleItemConfiguration.cs`
- Create: `src/Todo.Api/Persistence/Migrations/*`
- Modify: `src/Todo.Api/Extensions/ServiceCollectionExtensions.cs`
- Create: `src/Todo.Api/appsettings.json`
- Create: `src/Todo.Api/appsettings.Development.json`
- Create: `docker-compose.yml`

- [x] **Step 1: Create the persistence model, abstraction, and DbContext**

```csharp
// src/Todo.Api/Abstractions/Persistence/IApplicationDbContext.cs
using Microsoft.EntityFrameworkCore;
using Todo.Api.Persistence.Models;

namespace Todo.Api.Abstractions.Persistence;

public interface IApplicationDbContext
{
    DbSet<SampleItem> SampleItems { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

```csharp
// src/Todo.Api/Persistence/Models/SampleItem.cs
namespace Todo.Api.Persistence.Models;

public sealed class SampleItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
}
```

```csharp
// src/Todo.Api/Persistence/ApplicationDbContext.cs
using Microsoft.EntityFrameworkCore;
using Todo.Api.Abstractions.Persistence;
using Todo.Api.Persistence.Models;

namespace Todo.Api.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<SampleItem> SampleItems => Set<SampleItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
```

- [x] **Step 2: Add the EF Core mapping**

```csharp
// src/Todo.Api/Persistence/Configurations/SampleItemConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Todo.Api.Persistence.Models;

namespace Todo.Api.Persistence.Configurations;

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

- [x] **Step 3: Register PostgreSQL-backed persistence in the API project**

```csharp
// src/Todo.Api/Extensions/ServiceCollectionExtensions.cs
using System.Reflection;
using BuildingBlocks.Application.Behaviors;
using BuildingBlocks.Endpoints;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Todo.Api.Abstractions.Persistence;
using Todo.Api.Features.Sample.Create;
using Todo.Api.Persistence;

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
        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IApplicationDbContext>(serviceProvider => serviceProvider.GetRequiredService<ApplicationDbContext>());
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(apiAssembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(RequestLoggingBehavior<,>));
        });
        services.AddValidatorsFromAssembly(apiAssembly);

        return services;
    }
}
```

- [x] **Step 4: Generate the initial migration**

Run: `dotnet ef migrations add InitialCreate --project src/Todo.Api/Todo.Api.csproj --startup-project src/Todo.Api/Todo.Api.csproj --output-dir Persistence/Migrations`
Expected: migration files appear under `src/Todo.Api/Persistence/Migrations`

## Task 4: Implement the Sample Vertical Slices

**Files:**

- Create: `src/Todo.Api/Features/Sample/Create/*`
- Create: `src/Todo.Api/Features/Sample/GetById/*`
- Create: `src/Todo.Api/Features/Sample/List/*`
- Modify: `tests/Todo.Api.UnitTests/Features/Sample/Create/CreateSampleValidatorTests.cs`

- [x] **Step 1: Build the create slice against `IApplicationDbContext`**

```csharp
// src/Todo.Api/Features/Sample/Create/Handler.cs
using BuildingBlocks.Application.Results;
using MediatR;
using Todo.Api.Abstractions.Persistence;
using Todo.Api.Persistence.Models;

namespace Todo.Api.Features.Sample.Create;

public sealed class Handler(IApplicationDbContext dbContext) : IRequestHandler<Command, Result<Response>>
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

- [x] **Step 2: Build the read slices against `IApplicationDbContext`**

```csharp
// src/Todo.Api/Features/Sample/GetById/Handler.cs
using BuildingBlocks.Application.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Todo.Api.Abstractions.Persistence;

namespace Todo.Api.Features.Sample.GetById;

public sealed class Handler(IApplicationDbContext dbContext) : IRequestHandler<Query, Result<Response>>
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
// src/Todo.Api/Features/Sample/List/Handler.cs
using BuildingBlocks.Application.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Todo.Api.Abstractions.Persistence;

namespace Todo.Api.Features.Sample.List;

public sealed class Handler(IApplicationDbContext dbContext) : IRequestHandler<Query, Result<IReadOnlyList<Response>>>
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

- [x] **Step 3: Run unit tests to verify sample slices**

Run: `dotnet test tests/Todo.Api.UnitTests/Todo.Api.UnitTests.csproj -v minimal`
Expected: PASS

## Task 5: Wire the API Host and Cross-Cutting Behavior

**Files:**

- Create: `src/Todo.Api/Extensions/ServiceCollectionExtensions.cs`
- Create: `src/Todo.Api/Extensions/ApplicationExtensions.cs`
- Create: `src/Todo.Api/ExceptionHandling/GlobalExceptionHandler.cs`
- Modify: `src/Todo.Api/Program.cs`

- [x] **Step 1: Implement the host composition root**

```csharp
// src/Todo.Api/Program.cs
using BuildingBlocks.Endpoints;
using Scalar.AspNetCore;
using Serilog;
using Todo.Api.ExceptionHandling;
using Todo.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

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

- [x] **Step 2: Implement exception handling**

```csharp
// src/Todo.Api/ExceptionHandling/GlobalExceptionHandler.cs
using BuildingBlocks.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Todo.Api.ExceptionHandling;

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

- [x] **Step 3: Run the health integration test**

Run: `dotnet test tests/Todo.Api.IntegrationTests/Todo.Api.IntegrationTests.csproj --filter HealthEndpointTests -v minimal`
Expected: PASS

## Task 6: Add Unit and Integration Coverage

**Files:**

- Create: `tests/Todo.Api.UnitTests/Features/Sample/Create/CreateSampleHandlerTests.cs`
- Create: `tests/Todo.Api.UnitTests/Features/Sample/List/ListSamplesHandlerTests.cs`
- Create: `tests/Todo.Api.IntegrationTests/Fixtures/PostgresContainerFixture.cs`
- Create: `tests/Todo.Api.IntegrationTests/Fixtures/CustomWebApplicationFactory.cs`
- Create: `tests/Todo.Api.IntegrationTests/Features/Sample/SampleEndpointsTests.cs`

- [x] **Step 1: Write the unit tests against `Todo.Api.Persistence.ApplicationDbContext`**

```csharp
// tests/Todo.Api.UnitTests/Features/Sample/Create/CreateSampleHandlerTests.cs
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Todo.Api.Features.Sample.Create;
using Todo.Api.Persistence;

namespace Todo.Api.UnitTests.Features.Sample.Create;

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

        var result = await handler.Handle(new Command(" First sample "), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("First sample");
        dbContext.SampleItems.Should().ContainSingle(x => x.Name == "First sample");
    }
}
```

- [x] **Step 2: Write the PostgreSQL-backed integration host**

```csharp
// tests/Todo.Api.IntegrationTests/Fixtures/CustomWebApplicationFactory.cs
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Todo.Api.Persistence;

namespace Todo.Api.IntegrationTests.Fixtures;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgresContainerFixture _postgres = new(LoadConfiguredConnectionString());

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = _postgres.ConnectionString
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
            {
                var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                var connectionString = configuration.GetConnectionString("Postgres")
                    ?? throw new InvalidOperationException("Connection string 'Postgres' was not found.");

                options.UseNpgsql(connectionString);
            });
        });
    }
}
```

- [x] **Step 3: Run the full test suite**

Run: `dotnet test SuperPowerAI.sln -v minimal`
Expected: all unit and integration tests PASS

## Self-Review

- Spec coverage: the plan covers the modular monolith layout, Minimal API host, shared `/api/v1` route-group registration, MediatR CQRS flow, FluentValidation behavior, Serilog logging, PostgreSQL persistence inside `Todo.Api`, Scalar/OpenAPI, health checks, Docker Compose, and test coverage.
- Placeholder scan: no `TODO`, `TBD`, or deferred sections remain.
- Type consistency: the plan consistently uses `Todo.Api`, `IApplicationDbContext`, `ApplicationDbContext`, `Command`, `Query`, `Response`, `RequestValidationException`, and `ProblemDetails`.
