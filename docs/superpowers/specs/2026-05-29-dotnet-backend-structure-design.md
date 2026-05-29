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

The backend will use a modular monolith architecture with Minimal API at the application edge.

This balances:

- clear module and feature boundaries
- low operational complexity
- good fit for vertical-slice organization
- enough structure for production concerns without heavy boilerplate

## Architecture Overview

The solution will be organized into a small set of focused projects:

- `src/AppHost.Api`
  - application entry point
  - DI composition root
  - middleware registration
  - OpenAPI and Scalar setup
  - health checks
  - endpoint discovery and mapping
- `src/AppModules`
  - business-neutral modules and feature slices
  - initial `Sample` module used as the reference pattern
- `src/BuildingBlocks`
  - shared abstractions and cross-cutting primitives
  - endpoint contracts
  - result/error primitives
  - MediatR pipeline behaviors
- `src/Infrastructure`
  - EF Core persistence
  - PostgreSQL configuration
  - `DbContext`
  - entity configurations
  - migrations
  - persistence-related dependency registration
- `tests/AppHost.Api.IntegrationTests`
  - end-to-end API and infrastructure verification
- `tests/AppModules.UnitTests`
  - focused unit tests for handlers, validators, and slice behavior

## Vertical-Slice Structure

The codebase will be organized by feature first, not by technical layer.

The initial sample module will demonstrate the pattern with slices such as:

- `Features/Sample/Create`
- `Features/Sample/GetById`
- `Features/Sample/List`

Each slice should keep its behavior in one place. A slice may contain:

- `Command` or `Query`
- `Validator`
- `Handler`
- `Endpoint`
- `Response` if a dedicated response contract is needed

This avoids the common failure mode where a project claims to use vertical slices but still centralizes endpoints, validators, handlers, and DTOs into global layer folders.

## API Style

The API surface will use Minimal API.

Reasoning:

- it aligns well with vertical-slice organization
- endpoints can live beside their request/handler code
- it reduces ceremony compared with controller-based organization
- it keeps the starter easier to extend and copy from

Each feature endpoint will be mapped through a small contract such as `IEndpoint` so feature registration remains explicit and discoverable.

Routes will be namespaced under a shared `/api/v1` prefix defined once at the host or route-group level, not repeated inside every endpoint implementation. Full API versioning infrastructure is not required in the initial scaffold.

## CQRS Design

MediatR will be the application dispatch mechanism.

- write operations use `Command`
- read operations use `Query`
- Minimal API endpoints send requests through `ISender`
- handlers live inside their corresponding slice

This is CQRS at the application boundary and handler level. It does not require separate databases, separate deployments, or event-driven infrastructure at this stage.

## Validation and Pipeline Behaviors

Cross-cutting MediatR pipeline behaviors will be intentionally limited to:

- `ValidationBehavior<,>`
- `RequestLoggingBehavior<,>`

### ValidationBehavior

- runs all FluentValidation validators for a request before handler execution
- stops invalid requests before reaching business logic
- integrates with standardized API error responses

### RequestLoggingBehavior

- logs request execution boundaries and relevant metadata
- gives visibility into request flow without embedding logging in every handler

No transaction pipeline behavior will be added.

Command handlers are responsible for calling `SaveChangesAsync` when needed. This keeps behavior explicit and avoids adding infrastructure policy that is not currently required.

## Persistence Design

Persistence will use:

- `EF Core`
- `Npgsql`
- PostgreSQL

The database layer will live in `src/Infrastructure`.

Initial persistence structure:

- `Persistence/ApplicationDbContext.cs`
- `Persistence/Configurations/*`
- `Persistence/Migrations/*`

Because there is no real domain yet, the starter should use simple entities and straightforward EF Core mapping. It should avoid premature DDD complexity such as rich aggregates, domain event infrastructure, or advanced repository abstractions.

The starter should still keep persistence boundaries clean enough that business modules do not depend on raw infrastructure setup details.

Default local database settings:

- database name: `superpowerai`
- PostgreSQL port: `5433`

## Error Handling

The API host will provide centralized exception handling and use standard .NET `ProblemDetails` responses for failures.

Expected behavior:

- successful responses return plain response data and do not need to be wrapped in a custom envelope
- validation failures return `ValidationProblemDetails`
- explicit application failures produced from `Result.Failure(...)` are mapped to `ProblemDetails` at the endpoint boundary
- unhandled exceptions are mapped to safe server-error `ProblemDetails`
- API responses are consistent across slices and global exception handling

This keeps success responses simple while relying on the built-in .NET error contract for failures.

## API Documentation and UI

The starter will expose OpenAPI metadata and integrate Scalar as the API exploration UI.

Expected outcome:

- OpenAPI document generated from the API host
- Scalar UI enabled for local development
- users can inspect and call endpoints directly from the browser

## Observability and Operations

The starter should include basic production-ready operational support:

- structured logging with `Serilog`
- health checks
- environment-based configuration
- local PostgreSQL setup via Docker Compose

These are included because they are likely to be needed immediately in a real backend starter and are cheaper to establish now than retrofit later.

## Testing Strategy

The scaffold should include the test structure from the start.

### Unit Tests

`xUnit` is the default test framework for the starter at the current stage.

Unit tests focus on:

- validators
- MediatR handlers
- small slice-level behavior

### Integration Tests

Integration tests focus on:

- application bootstrapping
- endpoint behavior
- database integration
- infrastructure wiring

`WebApplicationFactory` should be used for API integration tests. `Testcontainers.PostgreSql` is a valid addition if full PostgreSQL-backed integration tests are included in the first pass.

## Initial Package Set

Expected core packages:

- `Microsoft.EntityFrameworkCore`
- `Npgsql.EntityFrameworkCore.PostgreSQL`
- `MediatR`
- `FluentValidation`
- `Scalar.AspNetCore`
- `Serilog.AspNetCore`
- `Microsoft.AspNetCore.OpenApi`
- `Microsoft.AspNetCore.Mvc.Testing`
- `xunit`

Optional but recommended for stronger integration testing:

- `Testcontainers.PostgreSql`

## Explicit Non-Goals for This Starter

The initial scaffold will not include:

- transaction pipeline behavior
- outbox implementation
- event bus or message broker integration
- authentication and authorization
- caching
- advanced domain-driven design patterns
- multiple business modules beyond the sample reference module

## Platform Baseline

The starter targets `.NET 10`.

These can be added later when real requirements justify them.

## Expected Outcome

The finished starter should let a developer add a new feature slice by following the sample pattern:

1. create a feature folder
2. add request, validator, handler, and endpoint
3. wire persistence through the existing infrastructure setup
4. expose the endpoint automatically through the existing registration approach

The codebase should remain understandable without requiring developers to navigate across many unrelated layer folders for a single feature.
