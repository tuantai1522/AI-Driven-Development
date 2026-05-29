# .NET Backend Structure Design

Date: 2026-05-29

## Goal

Scaffold a backend starter on .NET for future business domains with these constraints:

- .NET 10
- Solution/product name: `SuperPowerAI`
- PostgreSQL as the primary database
- Vertical-slice structure
- FluentValidation
- CQRS with MediatR
- Scalar for API exploration and testing
- No business domain modeling yet

The result should be a production-ready starter, not just a demo. It should be easy to extend with new modules and features without refactoring the base architecture.

## Chosen Direction

The backend uses a modular monolith with Minimal API at the application edge.

This balances:

- clear module and feature boundaries
- low operational complexity
- good fit for vertical-slice organization
- enough structure for production concerns without heavy boilerplate

The runtime code is intentionally consolidated into a small set of projects:

- `src/Todo.Api` owns the host, feature slices, persistence, DI, and EF Core migrations
- `src/BuildingBlocks` owns shared contracts and cross-cutting primitives
- `tests/*` mirror the runtime structure through unit and integration tests

No separate runtime persistence project is part of the target architecture.

## Architecture Overview

The current implementation is organized into these focused areas:

- `src/Todo.Api`
  - application entry point
  - DI composition root
  - middleware registration
  - OpenAPI and Scalar setup
  - health checks
  - endpoint discovery and mapping
  - business-neutral feature slices
  - EF Core persistence and migrations
  - initial `Sample` module used as the reference pattern
- `src/BuildingBlocks`
  - shared abstractions and cross-cutting primitives
  - endpoint contracts
  - result and error primitives
  - MediatR pipeline behaviors
- `tests/Todo.Api.UnitTests`
  - focused unit tests for validators, handlers, and slice behavior
- `tests/Todo.Api.IntegrationTests`
  - end-to-end API and PostgreSQL verification

## Vertical-Slice Structure

The codebase is organized by module first, then by slice, not by technical layer.

The initial sample module demonstrates the pattern with slices such as:

- `Features/Sample/Create`
- `Features/Sample/GetById`
- `Features/Sample/List`

Future business modules should follow the same shape, for example:

- `Features/Todo/Create`
- `Features/Todo/GetById`
- `Features/Todo/List`

Each slice may contain:

- `Command` or `Query`
- `Validator`
- `Handler`
- `Endpoint`
- `Response` when a dedicated response contract is useful

This keeps one feature's behavior in one place and avoids scattering endpoint, validator, handler, and DTO logic across global layer folders.

## API Style

The API surface uses Minimal API.

Reasoning:

- it aligns well with vertical-slice organization
- endpoints can live beside their request and handler code
- it reduces ceremony compared with controller-based organization
- it keeps the starter easier to extend and copy from

Feature endpoints are mapped through a small contract such as `IEndpoint` so registration remains explicit and discoverable.

Routes are namespaced under a shared `/api/v1` prefix defined once at the host or route-group level, not repeated inside every endpoint implementation.

## CQRS Design

MediatR is the application dispatch mechanism.

- write operations use `Command`
- read operations use `Query`
- Minimal API endpoints send requests through `ISender`
- handlers live inside their corresponding slice

This is CQRS at the application boundary and handler level. It does not require separate databases, separate deployments, or event-driven infrastructure at this stage.

## Validation and Pipeline Behaviors

Cross-cutting MediatR pipeline behaviors are intentionally limited to:

- `ValidationBehavior<,>`
- `RequestLoggingBehavior<,>`

### ValidationBehavior

- runs all FluentValidation validators for a request before handler execution
- stops invalid requests before reaching business logic
- integrates with standardized API error responses

### RequestLoggingBehavior

- logs request execution boundaries and relevant metadata
- gives visibility into request flow without embedding logging in every handler

No transaction pipeline behavior is added.

Command handlers are responsible for calling `SaveChangesAsync` when needed. This keeps write behavior explicit and avoids introducing broader persistence policy before it is justified.

## Persistence Design

Persistence uses:

- `EF Core`
- `Npgsql`
- PostgreSQL

The database layer lives inside `src/Todo.Api/Persistence`.

Initial persistence structure:

- `Abstractions/Persistence/IApplicationDbContext.cs`
- `Persistence/ApplicationDbContext.cs`
- `Persistence/Models/*`
- `Persistence/Configurations/*`
- `Persistence/Migrations/*`

EF Core migrations are executed with `src/Todo.Api` as both the project and startup project so configuration comes from the same path used at runtime.

Because there is no real business domain yet, the starter uses simple entities and straightforward EF Core mapping. It deliberately avoids richer DDD patterns such as aggregates, domain event infrastructure, or repository abstractions.

Default local database settings:

- database name: `superpowerai`
- PostgreSQL port: `5433`

## Error Handling

The API host provides centralized exception handling and uses standard .NET `ProblemDetails` responses for failures.

Expected behavior:

- successful responses return plain response data
- validation failures return `ValidationProblemDetails`
- explicit application failures produced from `Result.Failure(...)` are mapped to `ProblemDetails` at the endpoint boundary
- unhandled exceptions are mapped to safe server-error `ProblemDetails`

This keeps success responses simple while relying on the built-in .NET error contract for failures.

## API Documentation and UI

The starter exposes OpenAPI metadata and integrates Scalar as the local API exploration UI.

Expected outcome:

- an OpenAPI document generated from the API host
- Scalar enabled for local development
- endpoints inspectable and callable directly from the browser

## Observability and Operations

The starter includes basic production-ready operational support:

- structured logging with `Serilog`
- health checks
- environment-based configuration
- local PostgreSQL setup via Docker Compose

These are included because they are likely to be needed immediately in a real backend starter and are cheaper to establish now than retrofit later.

## Testing Strategy

The scaffold includes test structure from the start.

### Unit Tests

`xUnit` is the default unit test framework.

Unit tests focus on:

- validators
- MediatR handlers
- small slice-level behavior

### Integration Tests

Integration tests focus on:

- application bootstrapping
- endpoint behavior
- database integration
- runtime persistence wiring

`WebApplicationFactory` is used for API integration tests. `Testcontainers.PostgreSql` is part of the baseline because the starter already verifies against PostgreSQL rather than only using an in-memory substitute.

## Initial Package Set

Expected core packages:

- `Microsoft.EntityFrameworkCore`
- `Microsoft.EntityFrameworkCore.Design`
- `Npgsql.EntityFrameworkCore.PostgreSQL`
- `MediatR`
- `FluentValidation`
- `Scalar.AspNetCore`
- `Serilog.AspNetCore`
- `Microsoft.AspNetCore.OpenApi`
- `Microsoft.AspNetCore.Mvc.Testing`
- `xunit`

Recommended supporting packages:

- `FluentAssertions`
- `NSubstitute`
- `Testcontainers.PostgreSql`

## Explicit Non-Goals for This Starter

The initial scaffold does not include:

- transaction pipeline behavior
- outbox implementation
- event bus or message broker integration
- authentication and authorization
- caching
- advanced domain-driven design patterns
- multiple business modules beyond the sample reference module

## Platform Baseline

The starter targets `.NET 10`.

## Expected Outcome

The finished starter should let a developer add a new feature slice by following the sample pattern:

1. create a feature folder
2. add request, validator, handler, and endpoint
3. use the existing `IApplicationDbContext` and `ApplicationDbContext` for persistence
4. expose the endpoint automatically through the existing registration approach

The codebase should remain understandable without requiring developers to navigate across many unrelated layer folders for a single feature.
