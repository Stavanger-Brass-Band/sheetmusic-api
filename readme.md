# Sheet Music API

A REST API for managing Stavanger Brass Band's sheet music archive — projects, sheet music sets, parts (PDFs), categories, and musicians/users. Built with ASP.NET Core, EF Core / SQL Server, Azure Blob Storage and Azure AI Search.

## Tech stack

- **ASP.NET Core 10** (minimal hosting, controllers) with CQRS via [MediatR](https://github.com/jbogard/MediatR)
- **EF Core** on SQL Server for persistence, **ASP.NET Core Identity** for auth
- **Azure Blob Storage** for PDF part files, **Azure AI Search** for full-text part search
- **FluentValidation** for request validation
- **Asp.Versioning** for API versioning, **Scalar** + `Microsoft.AspNetCore.OpenApi` for interactive API docs
- **.NET Aspire** for local orchestration (SQL Server + Azurite containers)
- **xUnit** + **FluentAssertions** for integration testing

## Project structure

```
src/
├── SheetMusic.Api/              # The API itself (see below)
├── SheetMusic.Api.Test/         # xUnit integration tests, mirrors the domain layout
├── SheetMusic.AppHost/          # .NET Aspire orchestration for local development
└── SheetMusic.ServiceDefaults/  # Shared Aspire service defaults (telemetry, health checks, resilience)
```

`SheetMusic.Api` is organized by **domain** rather than by artifact type:

```
SheetMusic.Api/
├── Projects/    # Project, ProjectSheetMusicSet
├── Sets/        # SheetMusicSet, SheetMusicPart, Category
├── Parts/       # MusicPart, MusicPartAlias, part search index
├── Users/       # ApplicationUser, RefreshToken, Musician, Authorization/
└── Shared/      # Cross-cutting infrastructure: Database, Errors, BlobStorage, Search, Email, OData, Configuration
```

Each domain owns its own controller, `Commands/`, `Queries/`, `RequestModels/`, `ViewModels/`, `Entities/` and `Errors/`. See [.github/instructions/copilot-instructions.md](.github/instructions/copilot-instructions.md) for the full set of conventions used across the codebase.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (or another Aspire-compatible container runtime) — used to run SQL Server and the Azurite storage emulator locally
- The [Aspire CLI](https://learn.microsoft.com/dotnet/aspire/cli/overview) (`dotnet tool install -g Aspire.Cli` or `dotnet workload install aspire`), optional but recommended

## Running locally

The [SheetMusic.AppHost](src/SheetMusic.AppHost) project spins up SQL Server and an Azurite blob storage emulator in containers and wires up the API against them.

```powershell
cd src
aspire run
# or: dotnet run --project SheetMusic.AppHost
```

The Aspire dashboard shows the running resources and their logs. EF Core migrations and development data seeding run automatically on startup (see [DatabaseSeeder.cs](src/SheetMusic.Api/Database/DatabaseSeeder.cs)).

### Configuration & secrets

Some settings aren't safe to commit and must be supplied via [user secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) on the `SheetMusic.AppHost` project (or as environment variables), for example:

| Key | AppHost parameter | Purpose |
|---|---|---|
| `Resend:ApiKey` | `resend-api-key` | Transactional email (password reset, etc.) via [Resend](https://resend.com/). Leave unset locally to fall back to a no-op sender that logs instead of sending. |
| `Email:FromAddress`, `Email:FrontendBaseUrl` | `email-from-address`, `email-frontend-base-url` | Email sender address and link target for the member frontend |
| `Search:IndexPrefix` | `search-index-prefix` | Optional prefix (e.g. `test`, `prod`) so multiple environments can share one Azure AI Search service without one environment's index rebuild deleting another's. Unset preserves the historical unprefixed index name. |
| `Jwt:SigningKey` | `jwt-signing-key` | Symmetric key used to sign JWT access tokens. No committed fallback and no local-dev default - a missing value fails startup (AppHost) or app startup (API). Every environment, including local dev, must set its own value via user secrets. |

Set any of these via `dotnet user-secrets set Parameters:<parameter-name> <value>` from `src/SheetMusic.AppHost`. `jwt-signing-key` in particular must be set before running `aspire run` locally for the first time.

See [ConfigKeys.cs](src/SheetMusic.Api/Configuration/ConfigKeys.cs) for the full list of configuration keys, including rate limiting overrides.

### API documentation

Once running, browse to **`/scalar`** for the interactive API reference (powered by [Scalar](https://scalar.com/)), or fetch the raw OpenAPI document at `/openapi/{version}.json` (e.g. `/openapi/2.0.json`). Two API versions are currently supported: `1.0` (deprecated) and `2.0`.

Authenticate via `POST /token` (username/password, `Content-Type: application/x-www-form-urlencoded`) to get a bearer token — Scalar's Authentication panel has the OAuth2 password flow preselected so you can try this out interactively without leaving the docs.

## Running tests

```powershell
cd src
dotnet test
```

Integration tests spin up the real API in-process (via `WebApplicationFactory<Program>`) against an EF Core InMemory database, with fakes for blob storage, email and the search index. See [SheetMusicWebAppFactory.cs](src/SheetMusic.Api.Test/Infrastructure/SheetMusicWebAppFactory.cs).

## Database migrations

Migrations live under [Shared/Database/Migrations](src/SheetMusic.Api/Migrations). To add one after changing an entity:

```powershell
cd src/SheetMusic.Api
dotnet ef migrations add <Name>
```

Migrations are applied automatically at startup (unless the `SkipMigrations` configuration flag is set, as it is for the test host).

## Contributing

Commit messages follow [Conventional Commits](https://www.conventionalcommits.org/) — releases and the [changelog](CHANGELOG.md) are generated automatically by [release-please](https://github.com/googleapis/release-please) based on them.

