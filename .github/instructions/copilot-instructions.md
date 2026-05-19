---
description: Instructions for working with the Sheet Music API codebase
applyTo: 'src/**/*.cs'
---

# Sheet Music API - Copilot Instructions

ASP.NET Core 8.0 Web API for managing sheet music collections. Uses CQRS, MediatR, EF Core, Azure Blob Storage, and Azure Cognitive Search.

## Architecture

### CQRS with MediatR
- All business logic uses CQRS pattern with MediatR
- Commands modify state (e.g., `AddPart`, `UpdateSetMetadata`)
- Queries retrieve data (e.g., `GetPartCollection`, `GetSet`)
- Handlers are nested classes inside request classes
- Controllers only orchestrate - delegate to MediatR

### Error Handling
- Custom exceptions inherit from `ExceptionBase` with HTTP status codes
- `ErrorHandlerMiddleware` catches and converts to ProblemDetails
- Never throw generic exceptions - create specific error types

### Validation
- FluentValidation for all input validation
- Validators are nested classes in RequestModels named `Validator`

## Code Conventions

### Language Features
- C# 12 primary constructors for dependency injection
- Nullable reference types enabled
- `null!` for properties guaranteed non-null after initialization
- `LangVersion` latest

### Naming
- Controllers: `{Entity}Controller`
- Request models: `{Entity}Request` (in `Controllers/RequestModels/`)
- View models: `Api{Entity}` (in `Controllers/ViewModels/`)
- Commands: Verb-first (e.g., `AddPart`, `DeleteSet`)
- Queries: `Get{Entity}` (e.g., `GetPartCollection`)

### Controllers
- Mark with `[ApiController]`, `[Produces("application/json")]`
- Use `[Authorize("Admin")]` for admin-only endpoints
- Include XML documentation for Swagger
- Return `ActionResult<T>` or `ActionResult`
- Primary constructors with `IMediator mediator`

### Database
- EF Core with SQL Server
- `Guid` for entity IDs
- Always async operations: `ToListAsync()`, `FirstOrDefaultAsync()`
- Navigation properties nullable or initialized to empty collections

## Testing
- xUnit + FluentAssertions (`.Should().Be()`)
- `SheetMusicWebAppFactory` for integration tests
- Test naming: `{Method}_{ExpectedBehavior}_{Condition}`
- Auth: `factory.CreateClientWithTestToken(TestUser.Administrator)`

## File Organization
```
SheetMusic.Api/
├── Controllers/          # API controllers
│   ├── RequestModels/   # Input DTOs + Validators
│   └── ViewModels/      # Output DTOs (Api-prefixed)
├── CQRS/
│   ├── Command/         # State-changing operations
│   └── Query/           # Read operations
├── Database/Entities/   # EF Core entities
├── Errors/              # Custom exceptions
└── Repositories/        # Data access
```

## Key Principles
- Controllers delegate to MediatR only
- Never return entities directly (use ViewModels)
- Always use async/await
- Specific exceptions with proper status codes
- XML docs on all public APIs
- FluentValidation for input
- Integration tests for endpoints