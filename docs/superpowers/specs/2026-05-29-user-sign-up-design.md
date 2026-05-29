# User Sign-Up Design

Date: 2026-05-29

## Goal

Add a first user sign-up capability to the current backend with these constraints:

- required input: `email`, `password`, `userName`
- `email` must be unique
- `userName` must be unique
- `password` must never be stored in plain text
- the design should follow a minimal DDD style
- the implementation should not introduce a repository abstraction

The scope is intentionally limited to account creation. Login, token issuance, email verification, password reset, and authorization are out of scope.

## Chosen Direction

The feature will follow the existing vertical-slice pattern while introducing a small domain boundary for `User`.

This means:

- the HTTP/API flow still uses `Endpoint -> Command -> Validator -> Handler`
- the application handler injects `IApplicationDbContext` directly from `Todo.Api`
- `User` is modeled as a domain entity instead of a persistence-only record
- EF Core maps the `User` entity directly
- password hashing is delegated to an abstraction such as `IPasswordHasher`, backed by a custom implementation

This keeps the implementation aligned with the current codebase while avoiding a pure CRUD-style user model.

## Scope

The sign-up feature will provide:

- `POST /api/v1/auth/sign-up`
- request validation for `email`, `password`, and `userName`
- uniqueness checks for `email` and `userName`
- password hashing before persistence
- `201 Created` response with non-sensitive user data
- `409 Conflict` when `email` or `userName` already exists

The feature will not provide:

- login
- JWT or session issuance
- email confirmation
- forgot-password flow
- user profile management
- role or permission modeling

## Architecture Overview

The feature fits into the current modular monolith structure with minimal change:

- `src/Todo.Api`
  - owns the `User` domain entity and sign-up feature slice
  - owns the password hashing abstraction and concrete hasher
  - contains the application behavior for registration
  - owns EF Core configuration, persistence wiring, and migrations for this service

Expected new structure:

- `src/Todo.Api/Domain/Users/User.cs`
- `src/Todo.Api/Features/Auth/SignUp/Command.cs`
- `src/Todo.Api/Features/Auth/SignUp/Response.cs`
- `src/Todo.Api/Features/Auth/SignUp/Validator.cs`
- `src/Todo.Api/Features/Auth/SignUp/Handler.cs`
- `src/Todo.Api/Features/Auth/SignUp/Endpoint.cs`
- `src/Todo.Api/Abstractions/Security/IPasswordHasher.cs`
- `src/Todo.Api/Security/PasswordHasher.cs`
- `src/Todo.Api/Abstractions/Persistence/IApplicationDbContext.cs`
- `src/Todo.Api/Persistence/ApplicationDbContext.cs`
- `src/Todo.Api/Persistence/Configurations/UserConfiguration.cs`
- `src/Todo.Api/Persistence/Migrations/*`

If the current project already has a better local namespace or folder for security abstractions, that local pattern should win.

## Domain Model

`User` is the central domain entity for this feature.

Minimum persisted fields:

- `Id`
- `Email`
- `UserName`
- `PasswordHash`
- `CreatedAtUtc`

The entity should not accept a raw password. A factory such as `User.Register(...)` should be used so the handler always passes a hashed password into the entity.

In this version, creation-time validation stays outside the domain entity:

- request-shape validation stays in FluentValidation
- uniqueness validation stays in the application handler and database constraints
- `User.Register(...)` is a simple construction path rather than an invariant-enforcing guard layer

The domain model should not know how passwords are hashed and should not depend on EF-specific behavior.

## Application Flow

The request lifecycle is:

1. Client sends `POST /api/v1/auth/sign-up`.
2. Minimal API endpoint binds the request into the sign-up command.
3. FluentValidation rejects invalid transport-level input early.
4. The handler queries `IApplicationDbContext` to check whether `email` or `userName` already exists.
5. If either exists, the handler returns a conflict result describing the duplicated field.
6. If the request is valid, the handler hashes the password through `IPasswordHasher`.
7. The handler creates a new `User` through the domain factory.
8. The handler adds the user to the DbContext and calls `SaveChangesAsync`.
9. The endpoint returns `201 Created` with `id`, `email`, and `userName`.

No repository abstraction is introduced. The application layer depends directly on `IApplicationDbContext`, consistent with the current codebase direction.

## Validation Rules

Transport and application validation rules:

- `Email`
  - required
  - must match email format
- `Password`
  - required
  - minimum length: 8 characters
- `UserName`
  - required
  - minimum length: 3 characters
  - maximum length: 30 characters

Business validation rules:

- `Email` must be unique
- `UserName` must be unique

No stronger password policy is required in this first version.

Hashing rule:

- `Password` must be converted to a non-reversible hash before persistence
- the initial implementation may use a custom PBKDF2-based hasher instead of Microsoft Identity's hasher

## Persistence Design

EF Core will map `User` directly instead of mapping a separate persistence-only model.

Persistence requirements:

- add `DbSet<User>` to the application DbContext abstraction and implementation in `Todo.Api`
- configure lengths and required fields in `UserConfiguration`
- create unique indexes for `Email` and `UserName`
- add a migration for the new users table

The feature should check uniqueness before insert for a clear application response, but the database must still enforce uniqueness as the final guard.

## Error Handling

Expected error behavior:

- invalid request shape returns the existing validation error format used by the project
- duplicate `email` returns `409 Conflict`
- duplicate `userName` returns `409 Conflict`
- conflict responses should identify the duplicated field clearly enough for frontend handling

The design must also account for race conditions:

- if two requests pass the pre-check and one later hits a database unique constraint, the application should still translate that failure into `409 Conflict` rather than leaking a generic `500`

The exact mechanism can be implemented either in the handler or in a narrow EF-aware error translation path inside `Todo.Api`, but the externally visible API behavior should remain the same.

## API Contract

Request body:

```json
{
  "email": "user@example.com",
  "password": "secret123",
  "userName": "sampleUser"
}
```

Success response:

- status: `201 Created`
- body includes:
  - `id`
  - `email`
  - `userName`

The response must not include:

- `password`
- `passwordHash`
- internal timestamps unless there is a clear API need

## Testing Strategy

### Unit Tests

Add unit tests for:

- validator rejects missing `email`
- validator rejects invalid email format
- validator rejects password shorter than 8 characters
- validator rejects `userName` shorter than 3 characters
- validator rejects `userName` longer than 30 characters
- handler creates a user successfully
- handler returns conflict when email already exists
- handler returns conflict when user name already exists
- handler uses the password hasher before persistence
- custom password hasher returns a non-plain-text value

### Integration Tests

Add integration tests for:

- `POST /api/v1/auth/sign-up` returns `201 Created` for a valid request
- the created user is persisted in PostgreSQL
- duplicate email returns `409 Conflict`
- duplicate user name returns `409 Conflict`
- the API never returns the password hash

## Non-Goals

This design does not cover:

- authentication flows
- authorization
- refresh tokens
- email verification
- password reset
- account lockout
- audit trails beyond a basic creation timestamp

## Expected Outcome

After implementation, the codebase should support a clean first registration flow that:

- matches the existing vertical-slice architecture
- introduces a minimal but real domain boundary for `User`
- keeps the feature self-contained in `Todo.Api`, including EF mapping and migrations
- avoids adding repository abstractions the team does not want
- leaves a straightforward path for future login/auth work without rewriting the user creation flow
