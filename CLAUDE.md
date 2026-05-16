# CLAUDE.md — Agent Source of Truth

This file is loaded automatically by Claude Code at the start of every session.
It is the canonical reference for architecture decisions, patterns, and iteration targets.

---

## Project

Yuki Backend Technical Test — a blogging REST API in C# .NET 8.

**Endpoints:** `POST /post`, `GET /post?page=1&pageSize=10&includeAuthor=false`, `GET /post/{id}?includeAuthor=true`, `POST /author`  
**Entities:** `Post` (id, author_id, title, description, content), `Author` (id, name, surname)  
**Seeded authors:** `11111111-1111-1111-1111-111111111111` (Jane Doe), `22222222-2222-2222-2222-222222222222` (John Smith)  
**Swagger UI:** `http://localhost:5002/swagger`

---

## Architecture

```
Domain → Application → Infrastructure → API
```

- **Domain** (`src/BloggingSystem.Domain/`): zero dependencies. `Post` and `Author` aggregate roots with `UncommittedEvents`, reconstitution, domain events (`PostCreatedEvent`, `AuthorCreatedEvent`), `AuthorId` value object.
- **Application** (`src/BloggingSystem.Application/`): CQRS via MediatR. Ports (`IEventStore`, `IPostReadRepository`, `IAuthorReadRepository`, `IMessageSerializer`). `PostProjection` translates events → read model. Read models (`PostReadModel`, `AuthorReadModel`) defined here as plain POCOs.
- **Infrastructure** (`src/BloggingSystem.Infrastructure/`): `InMemoryEventStore` + `MartenEventStore` (PostgreSQL via Marten 7.x, config-switchable), EF Core InMemory `BloggingDbContext`, `JsonMessageSerializer` + `XmlMessageSerializer` (config-switchable), `DataSeeder` (seeds 2 authors via `IHostedService`), `InfrastructureServiceExtensions.AddInfrastructure(IConfiguration)`.
- **API** (`src/BloggingSystem.Api/`): Minimal APIs, Swashbuckle Swagger at `/swagger`. `public partial class Program {}` required for `WebApplicationFactory`.

**Hexagonal boundary rule:** Domain and Application never reference Infrastructure or API namespaces. Infrastructure implements Application ports. API wires everything via DI.

**Event Sourcing flow:** `CreatePostCommand` → `Post.Create()` raises `PostCreatedEvent` → append to `IEventStore` → write to `OutboxEvents` table → `PostProjection.ProjectAsync()` upserts EF Core read model synchronously. `OutboxProcessor` (BackgroundService) re-projects any entries that are still pending after a crash, guaranteeing the read model never silently drifts from the event log.

**Serialization Strategy pattern:** `IMessageSerializer` (Application port) — `JsonMessageSerializer` (default) or `XmlMessageSerializer`, selected via `Serialization:Format` in `appsettings.json`. No Application or API code changes needed to switch.

**Event Store Strategy pattern:** `IEventStore` (Application port) — `InMemoryEventStore` (default, no deps) or `MartenEventStore` (PostgreSQL via Marten), selected via `EventStore:Provider` in `appsettings.json`. No Application or Domain code changes needed to switch.

---

## Key Files

| File | Role |
|------|------|
| `src/BloggingSystem.Domain/Aggregates/Post/Post.cs` | Post aggregate root |
| `src/BloggingSystem.Domain/Aggregates/Author/Author.cs` | Author aggregate root |
| `src/BloggingSystem.Application/Commands/CreatePost/CreatePostCommandHandler.cs` | Write path |
| `src/BloggingSystem.Application/Queries/GetPostById/GetPostByIdQueryHandler.cs` | Read path |
| `src/BloggingSystem.Application/Projections/PostProjection.cs` | Event → read model |
| `src/BloggingSystem.Infrastructure/Persistence/EventStore/InMemoryEventStore.cs` | Write store |
| `src/BloggingSystem.Infrastructure/Persistence/ReadModel/BloggingDbContext.cs` | EF Core read model |
| `src/BloggingSystem.Infrastructure/Persistence/Seeding/DataSeeder.cs` | Author seed data |
| `src/BloggingSystem.Infrastructure/DependencyInjection/InfrastructureServiceExtensions.cs` | DI wiring |
| `src/BloggingSystem.Api/Program.cs` | App entry point + Swagger |
| `src/BloggingSystem.Api/Endpoints/CreatePostEndpoint.cs` | POST /post |
| `src/BloggingSystem.Api/Endpoints/GetPostEndpoint.cs` | GET /post/{id} |
| `src/BloggingSystem.Api/Middleware/CorrelationIdMiddleware.cs` | X-Correlation-ID header → Serilog LogContext |
| `src/BloggingSystem.Api/Middleware/GlobalExceptionHandler.cs` | RFC 7807 ProblemDetails for all exceptions |
| `src/BloggingSystem.Application/Behaviors/LoggingBehavior.cs` | MediatR pipeline: request/response logging |
| `src/BloggingSystem.Application/Behaviors/ValidationBehavior.cs` | MediatR pipeline: FluentValidation enforcement |
| `src/BloggingSystem.Application/Ports/IOutboxWriter.cs` | Port: write domain events to the outbox |
| `src/BloggingSystem.Infrastructure/Outbox/EfCoreOutboxWriter.cs` | Persists events to OutboxEvents table (EF Core) |
| `src/BloggingSystem.Infrastructure/Outbox/OutboxProcessor.cs` | BackgroundService: replays pending outbox entries on restart |

---

## Tests

179 tests, 0 failures. Run with `dotnet test`.

| Project | Count | Type |
|---------|-------|------|
| `BloggingSystem.Domain.Tests` | 26 | Pure unit |
| `BloggingSystem.Application.Tests` | 50 | Unit (NSubstitute mocks + concrete validators) |
| `BloggingSystem.Infrastructure.Tests` | 48 | Integration + DI registration |
| `BloggingSystem.Api.Tests` | 48 | Functional (WebApplicationFactory) |
| `BloggingSystem.Architecture.Tests` | 7 | Architecture (NetArchTest.Rules) |

**Test isolation:** `BloggingApiFactory` replaces the DbContext with a unique `Guid.NewGuid()` InMemory database per factory instance, overrides `IEventStore` to InMemory, clears health check registrations, and replaces JWT bearer with `TestAuthHandler` (always authenticated) so tests never touch PostgreSQL or need real tokens. `AnonymousBloggingApiFactory` keeps real JWT bearer for auth-specific 401 tests.

**Coverage target:** >90%. Run `dotnet test --collect:"XPlat Code Coverage"` to measure.

---

## Configuration

All switches live in `appsettings.json` (or environment-specific overrides like `appsettings.Development.json`).

| Key | Valid values | Default | Notes |
|-----|-------------|---------|-------|
| `Serialization:Format` | `json`, `xml` | `json` | Switches `IMessageSerializer` — `JsonMessageSerializer` or `XmlMessageSerializer`. No Application/API code changes needed. |
| `EventStore:Provider` | `inmemory`, `marten` | `inmemory` | Switches `IEventStore` — `InMemoryEventStore` (no deps) or `MartenEventStore` (PostgreSQL via Marten 7.x). No Application/Domain code changes needed. |
| `ReadModel:Provider` | `inmemory`, `postgresql` | `inmemory` | Switches `BloggingDbContext` — EF Core InMemory (no deps) or Npgsql (PostgreSQL). Migrations applied automatically on startup via `DatabaseMigrator`. |
| `ConnectionStrings:PostgreSQL` | Npgsql connection string | *(none)* | **Required** when `EventStore:Provider` is `marten` **or** `ReadModel:Provider` is `postgresql`. Throws `InvalidOperationException` at startup if absent. |

**Example — switch to XML + PostgreSQL (both stores):**
```json
{
  "Serialization": { "Format": "xml" },
  "EventStore": { "Provider": "marten" },
  "ReadModel": { "Provider": "postgresql" },
  "ConnectionStrings": {
    "PostgreSQL": "Host=localhost;Port=5432;Database=blogging;Username=postgres;Password=postgres"
  }
}
```

---

## NuGet Packages

| Project | Packages |
|---------|----------|
| Application | MediatR 12.x, FluentValidation 12.x, FluentValidation.DependencyInjectionExtensions 12.x, Microsoft.Extensions.Logging.Abstractions 8.x |
| Infrastructure | EF Core InMemory 8.x, Npgsql.EntityFrameworkCore.PostgreSQL 8.x, Marten 7.x, AspNetCore.HealthChecks.Npgsql 8.x, Microsoft.Extensions.Hosting.Abstractions 8.x |
| API | Swashbuckle.AspNetCore 6.x, FluentValidation 12.x, Serilog.AspNetCore 8.x, Serilog.Enrichers.Environment 3.x, Serilog.Enrichers.Thread 4.x |
| All test projects | xUnit, FluentAssertions 6.x, NSubstitute 5.x |
| Api.Tests | Microsoft.AspNetCore.Mvc.Testing 8.x |
| Architecture.Tests | NetArchTest.Rules 1.3.x |

---

## Iteration Targets

Remaining improvements, grouped by production-readiness dimension:

### Security
1. **Auth** — Add JWT bearer authentication; scope `POST /post` and `POST /author` to authenticated users. Consider an `ICurrentUserService` port so domain logic stays auth-agnostic.
2. **Input sanitisation** — Strip or reject HTML/script content in `title`, `description`, and `content` fields to prevent stored-XSS if output is ever rendered in a browser.
3. **Rate limiting** — Apply ASP.NET Core's built-in rate limiter (`AddRateLimiter`) to write endpoints to prevent abuse.
4. **Secret management** — Move the `ConnectionStrings:PostgreSQL` value out of `appsettings.json` into environment variables or a secrets manager (Azure Key Vault, AWS Secrets Manager) before shipping to production.

### Observability
5. ~~**Structured logging**~~ ✅ Serilog structured logging with `CorrelationId` enrichment via `X-Correlation-ID` header and `LoggingBehavior` MediatR pipeline. Config-driven minimum levels.
6. ~~**Health checks**~~ ✅ `/healthz/live` (liveness, no checks) and `/healthz/ready` (readiness, PostgreSQL probe tagged "ready") exposed with JSON response writer.
7. **Distributed tracing** — Wire `OpenTelemetry.Extensions.Hosting` with OTLP exporter so spans are visible in Jaeger/Tempo; instrument MediatR pipeline and EF Core queries.
8. **Metrics** — Expose Prometheus-compatible metrics (`/metrics`) via `prometheus-net.AspNetCore`; track request latency, command throughput, and projection lag.
9. **OpenAPI XML comments** — Enable `<GenerateDocumentationFile>true</GenerateDocumentationFile>` in the API project and wire XML docs into `AddSwaggerGen` for richer Swagger descriptions.

### Resilience
10. **Retry / circuit-breaker** — Wrap PostgreSQL calls (EF Core + Marten) with Polly retry and circuit-breaker policies to handle transient failures.
11. **Idempotent command handling** — Add an `IdempotencyKey` header; store processed command IDs to make `POST /post` and `POST /author` safe to retry without duplicate data.
12. ~~**Outbox pattern**~~ ✅ `OutboxEvent` table + `EfCoreOutboxWriter` + `OutboxProcessor` (BackgroundService). Events written to outbox before inline projection; on restart the processor re-projects any pending entries, closing the crash-between-append-and-project window.

### Scalability & Performance
13. **Async projection worker** — Decouple `PostProjection` from the write path; have a background worker consume events from Marten's async daemon so the command handler returns faster.
14. **Read-model caching** — Add a Redis-backed response cache for `GET /post/{id}` and `GET /post`; invalidate on `PostCreatedEvent`.
15. **`GET /author/{id}` query** — Add a read endpoint for authors so consumers don't need to embed author data in every post fetch.
16. **Cursor-based pagination** — Upgrade `GET /post` from page-based to keyset/cursor pagination (`?after=<createdAt>&limit=20`) for consistent performance at large offsets.

### Availability
17. **Graceful shutdown** — Configure `hostOptions.ShutdownTimeout` and honour `CancellationToken` in all hosted services so rolling deploys drain cleanly.
18. **Database migrations as init container** — Move `DatabaseMigrator` out of the API startup path into a Kubernetes init container or separate migration job to avoid blocking pod startup.

### Architecture Evolution — Standard Monolith → Modular Monolith

The current codebase is a well-layered hexagonal monolith. The natural next step toward independent deployability (and eventually microservices) is to decompose it into **vertical modules**, each owning its own domain, application, and infrastructure slice. The transition can be done incrementally without breaking changes.

**Target module structure:**

```
src/
  Modules/
    Posts/
      Posts.Domain/          # Post aggregate, PostId, PostCreatedEvent
      Posts.Application/     # CreatePostCommand, GetPostsQuery, IPostReadRepository, PostProjection
      Posts.Infrastructure/  # PostReadRepository, PostDbContext (owns "posts" schema)
    Authors/
      Authors.Domain/        # Author aggregate, AuthorId, AuthorCreatedEvent
      Authors.Application/   # CreateAuthorCommand, IAuthorReadRepository, AuthorProjection
      Authors.Infrastructure/# AuthorReadRepository, AuthorDbContext (owns "authors" schema)
  Shared/
    Shared.Domain/           # IDomainEvent, IDateTimeProvider, shared value objects
    Shared.Application/      # IEventStore, IMessageSerializer, ValidationBehavior, PagedResult<T>
    Shared.Infrastructure/   # InMemoryEventStore, MartenEventStore, serializers, DI extensions
  BloggingSystem.Api/        # Thin host — wires modules, Swagger, middleware
```

**Key steps:**

19. **Define module public contracts** — Each module exposes only its command/query records and response DTOs as its public API. Domain aggregates and read models are internal (`internal sealed class`).
20. **Schema isolation** — Give each module its own EF Core `DbContext` with a dedicated PostgreSQL schema (`posts` and `authors`). Remove the shared `BloggingDbContext` that crosses module boundaries.
21. **In-process integration events** — Replace `PostProjection` calling `IAuthorReadRepository` directly with a MediatR `INotificationHandler<AuthorCreatedEvent>` inside the Posts module, so modules communicate via events rather than direct port calls.
22. **Per-module DI registration** — Replace the single `AddInfrastructure()` call with `services.AddPostsModule(config)` and `services.AddAuthorsModule(config)`, each internally registering their own repositories, projections, and validators.
23. **Per-module Architecture tests** — Extend `LayerDependencyTests` to assert that `Posts.*` assemblies never reference `Authors.*` assemblies (and vice versa), enforcing the module boundary at compile time.
24. **Feature flags per module** — Once modules are self-contained, each can be toggled or deployed independently, enabling a low-risk path toward extracting a module into a microservice when load demands it.

> **Why this matters for the production dimensions above:** Schema isolation removes the shared-table write contention that limits scalability; module boundaries make it safe to apply different retry/cache policies per domain; independent `DbContext` per module allows schema migrations to be scoped and rolled back without affecting other modules.

---

## Commands

```bash
dotnet build                                         # build solution
dotnet test                                          # run all 179 tests
dotnet test --collect:"XPlat Code Coverage"          # with coverage
dotnet run --project src/BloggingSystem.Api          # run locally (port 5002)
docker compose up --build                            # run in Docker (port 8080)
```

---

## Constraints / Rules for Agents (Principal Refinement)

- **Hexagonal Integrity:** Use Architecture Tests to verify Domain/Application have zero external dependencies. Never leak Infrastructure types (like EF Core classes) into Application Ports.
- **Event Sourcing Truth:** All state changes MUST be captured via `Post.RaiseEvent()`. Command Handlers are forbidden from modifying state; they only trigger actions on Aggregates.
- **Serialization Isolation:** Endpoints must use `IMessageSerializer` to map DTOs. This ensures the XML/JSON toggle requirement is architecturally enforced at the API boundary.
- **Read-Model Projection:** Projections must be idempotent. Re-running the same event through `PostProjection` should not create duplicate records in the Read DB.
- **Explicit DTOs:** Never return `PostReadModel` directly to the API consumer. Create a specific `PostResponse` DTO to allow the API schema to evolve independently of the Read Model.
- **Test Quality:** 90% coverage is a floor, not a ceiling. Functional tests must include "Negative Paths" (e.g., GET /post/invalid-id returns 404 with a structured ProblemDetails response).
- Never add dependencies from Domain → Application, Application → Infrastructure, or any layer → API.
- All new infrastructure implementations must register via `InfrastructureServiceExtensions.AddInfrastructure()`.
- New endpoints must chain `.WithName()`, `.WithTags("Posts")`, `.Produces<T>()` for Swagger completeness.
- New tests must follow the existing pattern: unit tests mock ports via NSubstitute; functional tests use `BloggingApiFactory`.
- Do not skip `dotnet test` after changes — all 179 tests must remain green.