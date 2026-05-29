# SuperPowerAI Backend Starter Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build `SuperPowerAI`, a production-ready .NET 10 backend starter with PostgreSQL, vertical slices, MediatR CQRS, FluentValidation, Scalar, Serilog, and xUnit-based test coverage, using a sample feature as the reference pattern.

**Architecture:** The current implementation is a modular monolith with `Todo.Api` as the single API project that contains both the host and feature code, `BuildingBlocks` for shared primitives, and `Infrastructure` for EF Core and PostgreSQL wiring. Feature code uses a `Features/<Module>/<Slice>` layout inside `Todo.Api`, while tests mirror that structure under `tests`.

**Tech Stack:** .NET 10, ASP.NET Core Minimal API, EF Core, Npgsql, MediatR, FluentValidation, Scalar.AspNetCore, Serilog.AspNetCore, xUnit, FluentAssertions, Microsoft.AspNetCore.Mvc.Testing, Testcontainers.PostgreSql

---

## Current Implementation Layout

### Source Projects

- `src/Todo.Api/Todo.Api.csproj`
  Responsibility: API host, composition root, middleware, OpenAPI, Scalar, health checks, endpoint mapping, and feature slices.
- `src/Todo.Api/Program.cs`
  Responsibility: host bootstrapping and request pipeline.
- `src/Todo.Api/Extensions/ServiceCollectionExtensions.cs`
  Responsibility: DI registration for endpoints, MediatR handlers, validators, health checks, OpenAPI, and infrastructure.
- `src/Todo.Api/Extensions/ApplicationExtensions.cs`
  Responsibility: middleware pipeline wiring.
- `src/Todo.Api/ExceptionHandling/GlobalExceptionHandler.cs`
  Responsibility: centralized exception-to-ProblemDetails mapping.
- `src/Todo.Api/Features/Sample/Create/*`
  Responsibility: sample create slice.
- `src/Todo.Api/Features/Sample/GetById/*`
  Responsibility: sample single-item query slice.
- `src/Todo.Api/Features/Sample/List/*`
  Responsibility: sample list query slice.

Future modules should follow the same shape, for example:

- `src/Todo.Api/Features/Todo/Create/*`
- `src/Todo.Api/Features/Todo/GetById/*`
- `src/Todo.Api/Features/Todo/List/*`

### Shared Projects

- `src/BuildingBlocks/BuildingBlocks.csproj`
  Responsibility: shared endpoint contracts, result primitives, exceptions, and MediatR behaviors.
- `src/Infrastructure/Infrastructure.csproj`
  Responsibility: EF Core, PostgreSQL configuration, DI wiring, persistence abstractions, entities, and migrations.
- `src/Infrastructure/Abstractions/IApplicationDbContext.cs`
  Responsibility: database abstraction consumed by feature handlers.
- `src/Infrastructure/Persistence/ApplicationDbContext.cs`
  Responsibility: EF Core `DbContext`.
- `src/Infrastructure/Persistence/Models/SampleItem.cs`
  Responsibility: sample persistence entity.
- `src/Infrastructure/Persistence/Configurations/SampleItemConfiguration.cs`
  Responsibility: EF mapping for `SampleItem`.
- `src/Infrastructure/Persistence/Migrations/*`
  Responsibility: PostgreSQL schema history.

### Test Projects

- `tests/Todo.Api.UnitTests/Todo.Api.UnitTests.csproj`
  Responsibility: validator and handler unit tests.
- `tests/Todo.Api.UnitTests/Features/Sample/Create/CreateSampleValidatorTests.cs`
- `tests/Todo.Api.UnitTests/Features/Sample/Create/CreateSampleHandlerTests.cs`
- `tests/Todo.Api.UnitTests/Features/Sample/List/ListSamplesHandlerTests.cs`
  Responsibility: slice-level correctness for the sample feature.

- `tests/Todo.Api.IntegrationTests/Todo.Api.IntegrationTests.csproj`
  Responsibility: end-to-end API and infrastructure verification.
- `tests/Todo.Api.IntegrationTests/Fixtures/PostgresContainerFixture.cs`
  Responsibility: PostgreSQL container lifecycle.
- `tests/Todo.Api.IntegrationTests/Fixtures/CustomWebApplicationFactory.cs`
  Responsibility: host bootstrapping for integration tests.
- `tests/Todo.Api.IntegrationTests/System/HealthEndpointTests.cs`
  Responsibility: host health verification.
- `tests/Todo.Api.IntegrationTests/Features/Sample/SampleEndpointsTests.cs`
  Responsibility: sample endpoint round-trip verification.

---

## Developer Commands

### Build

```bash
dotnet build SuperPowerAI.sln
```

### Test

```bash
dotnet test SuperPowerAI.sln
```

### Run API

```bash
dotnet run --project src/Todo.Api
```

### Add Migration

```bash
dotnet ef migrations add InitialCreate --project src/Infrastructure --startup-project src/Todo.Api --output-dir Persistence/Migrations
```

---

## Implementation Notes

- Feature discovery is assembly-based and currently scans the `Todo.Api` assembly.
- Endpoint routes are grouped under `/api/v1` once at the host layer.
- Success responses return plain data; failures use `ProblemDetails` or `ValidationProblemDetails`.
- `Infrastructure` owns persistence-facing types such as `IApplicationDbContext` and `SampleItem` to avoid circular project references.
- Unit tests mirror feature paths.
- Integration tests separate reusable fixtures from feature/system coverage.

---

## Verification Snapshot

- `dotnet build SuperPowerAI.sln --no-restore`
- `dotnet test SuperPowerAI.sln`

Both commands pass against the current layout.
