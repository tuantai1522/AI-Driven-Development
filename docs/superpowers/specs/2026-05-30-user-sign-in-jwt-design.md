# User Sign-In JWT Design

Date: 2026-05-30

## Goal

Add a first user sign-in capability to the current backend with these constraints:

- required input: `email`, `password`
- authentication uses JWT access tokens
- successful sign-in returns a bearer token in the response body
- JWT contains `sub`, `email`, and `unique_name`
- invalid credentials return a generic error without revealing whether the email exists
- access token lifetime is configuration-driven, with a default of 60 minutes

The scope is intentionally limited to issuing an access token for an existing user. Refresh tokens, cookie auth, role-based authorization, email verification, password reset, and logout are out of scope.

## Chosen Direction

The feature will follow the existing vertical-slice pattern while introducing a small JWT boundary in the security layer.

This means:

- the HTTP/API flow uses `Endpoint -> Command -> Validator -> Handler`
- the application handler injects `IApplicationDbContext`, `IPasswordHasher`, and `IJwtTokenGenerator`
- password verification stays behind the password hasher abstraction instead of being implemented in the handler
- JWT creation is delegated to a dedicated service so feature code does not depend on token-building details
- authentication middleware is configured centrally so future protected endpoints can reuse the same setup

This keeps the implementation aligned with the current codebase while avoiding auth logic spreading across handlers and endpoints.

## Scope

The sign-in feature will provide:

- `POST /api/v1/auth/sign-in`
- request validation for `email` and `password`
- credential validation against persisted users
- JWT access token issuance for valid credentials
- `200 OK` response with `accessToken`
- `401 Unauthorized` for invalid credentials with a single generic problem type
- centralized JWT authentication configuration for later authorized endpoints

The feature will not provide:

- refresh tokens
- HttpOnly auth cookies
- logout or token revocation
- role claims or permission claims
- lockout, rate limiting, or MFA
- protected business endpoints in this same change

## Architecture Overview

The feature fits into the current modular monolith structure with minimal change:

- `src/Todo.Api`
  - owns the sign-in feature slice
  - owns security abstractions for password verification and JWT generation
  - owns authentication middleware registration and JWT options binding
  - owns auth-specific error definitions

Expected new or changed structure:

- `src/Todo.Api/Features/Auth/SignIn/Command.cs`
- `src/Todo.Api/Features/Auth/SignIn/Response.cs`
- `src/Todo.Api/Features/Auth/SignIn/Validator.cs`
- `src/Todo.Api/Features/Auth/SignIn/Handler.cs`
- `src/Todo.Api/Features/Auth/SignIn/Endpoint.cs`
- `src/Todo.Api/Abstractions/Security/IPasswordHasher.cs`
- `src/Todo.Api/Abstractions/Security/IJwtTokenGenerator.cs`
- `src/Todo.Api/Security/PasswordHasher.cs`
- `src/Todo.Api/Security/Jwt/JwtOptions.cs`
- `src/Todo.Api/Security/Jwt/JwtTokenGenerator.cs`
- `src/Todo.Api/Extensions/ServiceCollectionExtensions.cs`
- `src/Todo.Api/Extensions/ApplicationBuilderExtensions.cs` or the existing pipeline extension point used by the app
- `src/Todo.Api/Features/Auth/AuthErrors.cs`
- `src/Todo.Api/appsettings.json`
- auth-related unit and integration tests under `tests/Todo.Api.UnitTests` and `tests/Todo.Api.IntegrationTests`

If a nearby file already owns middleware registration, that local pattern should win over creating a brand-new extension point.

## Application Flow

The request lifecycle is:

1. Client sends `POST /api/v1/auth/sign-in`.
2. Minimal API endpoint binds the request into the sign-in command.
3. FluentValidation rejects invalid transport-level input early.
4. The handler trims and normalizes the incoming email consistently with sign-up behavior.
5. The handler queries `IApplicationDbContext` for a matching user by email.
6. If no user exists, the handler returns `AuthErrors.InvalidCredentials`.
7. If the user exists but `IPasswordHasher.Verify` fails, the handler returns `AuthErrors.InvalidCredentials`.
8. If the credentials are valid, the handler calls `IJwtTokenGenerator` to issue an access token using the user id, email, and user name.
9. The endpoint returns `200 OK` with the token payload.

The handler remains an orchestration unit. It should not know how PBKDF2 verification or JWT signing works internally.

## Security Design

### Password Verification

The existing password hasher abstraction currently supports hashing only. It should be extended with a verification method, for example:

- `string Hash(string password)`
- `bool Verify(string password, string passwordHash)`

The concrete implementation should parse and verify the stored PBKDF2 format already written by sign-up, rather than replacing the current password storage format.

### JWT Generation

JWT generation should be encapsulated behind `IJwtTokenGenerator`.

Input to the generator should be the minimum user identity data needed to issue claims:

- user id
- email
- user name

Output should include:

- `accessToken`

JWT claims should be:

- `sub` = user id
- `email` = user email
- `unique_name` = user name

No `role` claim is needed in this version.

### JWT Configuration

JWT settings should come from configuration, with a dedicated section such as:

- `Jwt:Issuer`
- `Jwt:Audience`
- `Jwt:SigningKey`
- `Jwt:AccessTokenLifetimeMinutes`

Behavioral rules:

- `AccessTokenLifetimeMinutes` defaults to 60 minutes when not explicitly overridden
- `SigningKey` must come from configuration, not a hard-coded constant
- token signing should use a standard symmetric algorithm such as `HmacSha256`

## Validation Rules

Transport validation rules:

- `Email`
  - required
  - must match email format
- `Password`
  - required

Business validation rules:

- credentials must match an existing user
- failed authentication must always surface as one generic error

This version does not add account lockout, throttling, or advanced password policy enforcement at sign-in time.

## Error Handling

Expected error behavior:

- invalid request shape returns the existing validation error format used by the project
- unknown email returns `401 Unauthorized`
- wrong password returns `401 Unauthorized`
- both cases map to the same error type: `auth.invalid_credentials`

`AuthErrors` should add:

- code/type: `auth.invalid_credentials`
- message/detail: a generic credential failure message
- error type category: unauthorized

The API must not reveal whether the email exists. That rule is part of the public contract, not just an implementation detail.

## API Contract

Request body:

```json
{
  "email": "user@example.com",
  "password": "secret123"
}
```

Success response:

- status: `200 OK`
- body includes:
  - `accessToken`

Recommended response shape:

```json
{
  "accessToken": "<jwt>"
}
```

Failure response:

- status: `401 Unauthorized`
- body uses the existing `ProblemDetails` mapping with:
  - `title`: `Unauthorized`
  - `type`: `auth.invalid_credentials`
  - `detail`: generic invalid credential message

## Middleware and Runtime Wiring

The application should register JWT bearer authentication centrally so the token produced by sign-in is immediately usable by later protected endpoints.

Required runtime behavior:

- bind JWT options from configuration
- register `IJwtTokenGenerator`
- register JWT bearer authentication validation parameters using the configured issuer, audience, and signing key
- add authorization services
- run authentication middleware before authorization middleware in the HTTP pipeline

This change does not need to mark any existing endpoint as protected yet.

## Testing Strategy

### Unit Tests

Add unit tests for:

- validator rejects missing `email`
- validator rejects invalid email format
- validator rejects missing `password`
- password hasher verify returns true for a correct password
- password hasher verify returns false for an incorrect password
- handler returns success with token data for valid credentials
- handler returns unauthorized when the email does not exist
- handler returns unauthorized when the password is incorrect
- handler calls the JWT generator only for valid credentials

### Integration Tests

Add integration tests for:

- `POST /api/v1/auth/sign-in` returns `200 OK` for valid credentials after a user is created
- success response contains `accessToken`
- the returned JWT can be validated and contains `sub`, `email`, and `unique_name`
- invalid email returns `401 Unauthorized`
- wrong password returns `401 Unauthorized`
- invalid credential responses use `auth.invalid_credentials`

The integration tests may decode the JWT using the same test configuration signing key, but they should verify externally visible behavior rather than duplicating the generator internals line by line.

## Non-Goals

This design does not cover:

- refresh token storage and rotation
- cookie-based authentication
- endpoint authorization policies
- admin roles or claim enrichment
- logout and token revocation
- lockout and brute-force mitigations
- federation or external identity providers

## Expected Outcome

After implementation, the codebase should support a clean first sign-in flow that:

- matches the existing vertical-slice architecture
- reuses the existing password storage approach without breaking sign-up
- introduces a small, reusable JWT issuance boundary
- returns a client-consumable bearer token for valid credentials
- leaves a straightforward path for future `[Authorize]` endpoints without rewriting auth foundations
