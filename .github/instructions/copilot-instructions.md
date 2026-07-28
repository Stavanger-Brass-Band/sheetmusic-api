---
description: Instructions for working with the Sheet Music API codebase
applyTo: 'src/**/*.cs'
---

# Sheet Music API - Copilot Instructions

ASP.NET Core 8.0 Web API for managing sheet music collections. Uses CQRS, MediatR, EF Core, Azure Blob Storage, and Azure Cognitive Search.

## Architecture

### Domain-oriented folders (vertical slices)
- Source is organized by **domain**, not by artifact type
- Each domain owns its controller, commands, queries, request models, view models, entities and errors
- Domains: `Projects/`, `Sets/`, `Parts/`, `Users/`, plus `Shared/` for cross-cutting infrastructure
- Namespaces follow folders: `SheetMusic.Api.Projects.Commands`, `SheetMusic.Api.Users.Entities`, etc.
- Put a type in `Shared/` only if more than one domain uses it; otherwise it belongs in the owning domain
- Avoid new domain-to-domain dependencies (existing `Sets` <-> `Parts` coupling is accepted)

> Migration in progress: tracked by issue #192. Some folders may still use the legacy artifact-type layout (`Controllers/`, `CQRS/`, `Database/Entities/`, `Repositories/`). New code goes in the domain layout; when touching legacy files, follow the layout that file currently lives in unless the move is part of the tracked refactor.

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
- Controllers: `{Entity}Controller` (in `{Domain}/`)
- Request models: `{Entity}Request` (in `{Domain}/RequestModels/`)
- View models: `Api{Entity}` (in `{Domain}/ViewModels/`)
- Commands: Verb-first (e.g., `AddPart`, `DeleteSet`) in `{Domain}/Commands/`
- Queries: `Get{Entity}` (e.g., `GetPartCollection`) in `{Domain}/Queries/`

### Controllers
- Mark with `[ApiController]`, `[Produces("application/json")]`
- Use `[Authorize("Admin")]` for admin-only endpoints
- Include XML documentation for Swagger
- Return `ActionResult<T>` or `ActionResult`
- Primary constructors with `IMediator mediator`

### REST endpoint documentation
`PartsController` and `CategoriesController` are the reference standard - follow their shape for every controller/action:
- Controller-level `<summary>` describing the resource and any privilege requirements
- Per-action `<summary>` describing what the endpoint does
- `<param>` for every route/query/body parameter
- `<response code="...">` for every status code the action can produce (success plus `400`/`401`/`403`/`404`/`409`, etc.), matching the `StatusCode` of exceptions the underlying command/query can throw
- Prefer `ActionResult<T>` over `IActionResult` so Swagger can infer the response schema

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
- **Mandatory**: every code change (new endpoint, new field, bug fix, behavior change) must add or update a test in the same change. Do not defer tests as a "suggestion" - implement them.
- When adding a field to a request/view model, update or add a test asserting it round-trips through the relevant endpoint(s).
- Test-only view models live in `SheetMusic.Api.Test/Models` and must be kept in sync with the corresponding `Api{Entity}` ViewModel in the main project.

## File Organization
```
SheetMusic.Api/
├── Projects/                # Project, ProjectSheetMusicSet
│   ├── ProjectsController.cs
│   ├── Commands/            # State-changing operations
│   ├── Queries/             # Read operations
│   ├── RequestModels/       # Input DTOs + nested Validators
│   ├── ViewModels/          # Output DTOs (Api-prefixed)
│   ├── Entities/            # EF Core entities owned by the domain
│   └── Errors/              # Domain-specific exceptions
├── Sets/                    # SheetMusicSet, SheetMusicPart, Category
├── Parts/                   # MusicPart, MusicPartAlias, part search index
├── Users/                   # ApplicationUser, RefreshToken, Musician, Authorization/
└── Shared/                  # Cross-cutting infrastructure only
    ├── Database/            # SheetMusicContext, DatabaseSeeder, Migrations
    ├── Errors/              # ExceptionBase, ErrorHandlerMiddleware, generic errors
    ├── BlobStorage/
    ├── Search/              # Index infrastructure
    ├── Email/
    ├── OData/
    ├── Configuration/
    └── Utilities/
```

Tests mirror the domain layout: `SheetMusic.Api.Test/Tests/{Domain}/`.

EF Core migrations are **not** split per domain - they stay in a single ordered folder under `Shared/Database/Migrations`.

## Key Principles
- Controllers delegate to MediatR only
- Never return entities directly (use ViewModels)
- Always use async/await
- Specific exceptions with proper status codes
- XML docs on all public APIs
- FluentValidation for input
- Tests are mandatory for every code change, not optional - add or update them alongside the implementation

## Generated XML Documentation Files
- `SheetMusic.Api.xml` (and similarly named `.xml` files in other project folders) are compiler-generated from XML doc comments via `<GenerateDocumentationFile>true</GenerateDocumentationFile>` in the `.csproj`
- This applies to all build configurations (Debug and Release), so the file is always regenerated into `bin`/`obj` during any build, including cloud/CI builds
- They are loaded at runtime via `IncludeXmlComments` to populate Swagger/OpenAPI descriptions
- These files are pure build artifacts - do not commit them, and do not remove them from `.gitignore`