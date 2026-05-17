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

**Modular Monolith** — 10-project structure enforced at compile time via separate assemblies. Modules communicate via MediatR integration events; cross-module type contracts flow through `Authors.Contracts`, not through `Shared.Domain`.

```
src/
  Modules/
    Posts/
      Posts.Domain/          # Post aggregate, PostId, PostCreatedEvent, KnownAuthorNotFoundException
      Posts.Application/     # CreatePostCommand, GetPostsQuery, IPostReadRepository, PostProjection
      Posts.Infrastructure/  # PostsDbContext ("posts" schema), EfCoreOutboxWriter, OutboxProcessor, AuthorCreatedConsumer
    Authors/
      Authors.Contracts/     # AuthorId value object, AuthorCreatedEvent (cross-module integration contract)
      Authors.Domain/        # Author aggregate (depends on Authors.Contracts)
      Authors.Application/   # CreateAuthorCommand, IAuthorReadRepository, AuthorProjection
      Authors.Infrastructure/# AuthorsDbContext ("authors" schema), AuthorSeeder (publishes AuthorCreatedEvent via MassTransit)
  Shared/
    Shared.Domain/           # IDomainEvent (extends INotification), DomainException only — no module types
    Shared.Application/      # IEventStore, IOutboxWriter, IDateTimeProvider, IIntegrationEventPublisher, ValidationBehavior, LoggingBehavior, PagedResult<T>
    Shared.Infrastructure/   # InMemoryEventStore, MartenEventStore, JsonMessageSerializer, XmlMessageSerializer, MassTransitIntegrationEventPublisher, AddSharedInfrastructure()
  BloggingSystem.Api/        # Thin host — wires modules, Swagger, middleware, GlobalExceptionHandler
```

**Dependency direction:** `Authors.Contracts` → `Shared.Domain` only; `*.Domain` → `Shared.Domain` + (Posts.Domain → `Authors.Contracts`); `*.Application` → `*.Domain` + `Shared.*` + `Authors.Contracts`; `*.Infrastructure` → `*.Application` + `Shared.Infrastructure`; `BloggingSystem.Api` → all modules (wiring only).

**Cross-module communication:** `AuthorCreatedEvent` (defined in `Authors.Contracts`) is published via **MassTransit** (`IIntegrationEventPublisher` port → `MassTransitIntegrationEventPublisher` adapter) after an author is created. `Posts.Infrastructure.Consumers.AuthorCreatedConsumer` (MassTransit `IConsumer<AuthorCreatedEvent>`) maintains a local `KnownAuthors` table in `PostsDbContext` so `CreatePostCommandHandler` can validate the author without calling `Authors.*` directly. If the author is not found locally, `KnownAuthorNotFoundException` (Posts.Domain) is thrown — not `AuthorNotFoundException` — because Posts is checking its own cache, not the Authors bounded context.

**Schema isolation:** `PostsDbContext` owns schema `posts` (Posts, KnownAuthors, OutboxEvents tables). `AuthorsDbContext` owns schema `authors` (Authors table).

**API** (`src/BloggingSystem.Api/`): Minimal APIs, Swashbuckle Swagger at `/swagger`. `public partial class Program {}` required for `WebApplicationFactory`.

**Hexagonal boundary rule:** Domain and Application never reference Infrastructure or API namespaces. Infrastructure implements Application ports. API wires everything via DI.

**Event Sourcing flow:** `CreatePostCommand` → `Post.Create()` raises `PostCreatedEvent` → append to `IEventStore` → write to `OutboxEvents` table → `PostProjection.ProjectAsync()` upserts EF Core read model synchronously. `OutboxProcessor` (BackgroundService) re-projects any entries that are still pending after a crash, guaranteeing the read model never silently drifts from the event log.

**Serialization Strategy pattern:** `IMessageSerializer` (Application port) — `JsonMessageSerializer` (default) or `XmlMessageSerializer`, selected via `Serialization:Format` in `appsettings.json`. No Application or API code changes needed to switch.

**Event Store Strategy pattern:** `IEventStore` (Application port) — `InMemoryEventStore` (default, no deps) or `MartenEventStore` (PostgreSQL via Marten), selected via `EventStore:Provider` in `appsettings.json`. No Application or Domain code changes needed to switch.

---

## Key Files

| File | Role |
|------|------|
| `src/Modules/Posts/Posts.Domain/Aggregates/Post.cs` | Post aggregate root |
| `src/Modules/Authors/Authors.Domain/Aggregates/Author.cs` | Author aggregate root |
| `src/Modules/Posts/Posts.Application/Commands/CreatePost/CreatePostCommandHandler.cs` | Write path |
| `src/Modules/Posts/Posts.Application/Queries/GetPostById/GetPostByIdQueryHandler.cs` | Read path |
| `src/Modules/Posts/Posts.Application/Projections/PostProjection.cs` | Event → read model |
| `src/Modules/Posts/Posts.Infrastructure/Consumers/AuthorCreatedConsumer.cs` | Cross-module: MassTransit consumer that populates KnownAuthors from AuthorCreatedEvent |
| `src/Shared/Shared.Application/Ports/IIntegrationEventPublisher.cs` | Port: publish integration events to the message bus |
| `src/Shared/Shared.Infrastructure/Messaging/MassTransitIntegrationEventPublisher.cs` | Adapter: publishes via MassTransit IPublishEndpoint |
| `src/Shared/Shared.Infrastructure/EventStore/InMemoryEventStore.cs` | In-memory event store |
| `src/Shared/Shared.Infrastructure/EventStore/MartenEventStore.cs` | PostgreSQL event store (Marten) |
| `src/Modules/Posts/Posts.Infrastructure/Persistence/PostsDbContext.cs` | Posts + KnownAuthors + OutboxEvents (schema: posts) |
| `src/Modules/Authors/Authors.Infrastructure/Persistence/AuthorsDbContext.cs` | Authors (schema: authors) |
| `src/Modules/Authors/Authors.Infrastructure/Seeding/AuthorSeeder.cs` | Seeds 2 authors + publishes AuthorCreatedEvent |
| `src/Modules/Posts/Posts.Infrastructure/DependencyInjection/PostsModuleExtensions.cs` | AddPostsModule() |
| `src/Modules/Authors/Authors.Infrastructure/DependencyInjection/AuthorsModuleExtensions.cs` | AddAuthorsModule() |
| `src/Shared/Shared.Infrastructure/DependencyInjection/SharedInfrastructureExtensions.cs` | AddSharedInfrastructure() |
| `src/BloggingSystem.Api/Program.cs` | App entry point + Swagger |
| `src/BloggingSystem.Api/Endpoints/CreatePostEndpoint.cs` | POST /post |
| `src/BloggingSystem.Api/Endpoints/GetPostEndpoint.cs` | GET /post/{id} |
| `src/BloggingSystem.Api/Middleware/CorrelationIdMiddleware.cs` | X-Correlation-ID header → Serilog LogContext |
| `src/BloggingSystem.Api/Middleware/GlobalExceptionHandler.cs` | RFC 7807 ProblemDetails for all exceptions |
| `src/Shared/Shared.Application/Behaviors/LoggingBehavior.cs` | MediatR pipeline: request/response logging |
| `src/Shared/Shared.Application/Behaviors/ValidationBehavior.cs` | MediatR pipeline: FluentValidation enforcement |
| `src/Shared/Shared.Application/Ports/IOutboxWriter.cs` | Port: write domain events to the outbox |
| `src/Modules/Posts/Posts.Infrastructure/Outbox/EfCoreOutboxWriter.cs` | Persists events to OutboxEvents table (EF Core) |
| `src/Modules/Posts/Posts.Infrastructure/Outbox/OutboxProcessor.cs` | BackgroundService: replays pending outbox entries on restart |

---

## Tests

195 tests, 0 failures. Run with `dotnet test`.

| Project | Count | Type |
|---------|-------|------|
| `BloggingSystem.Domain.Tests` | 26 | Pure unit |
| `BloggingSystem.Application.Tests` | 50 | Unit (NSubstitute mocks + concrete validators) |
| `BloggingSystem.Infrastructure.Tests` | 47 | Integration + DI registration |
| `BloggingSystem.Api.Tests` | 48 | Functional (WebApplicationFactory) |
| `BloggingSystem.Architecture.Tests` | 24 | Architecture (NetArchTest.Rules, 24 cross-module boundary assertions including MassTransit guard) |

**Test isolation:** `BloggingApiFactory` sets environment to `"Testing"` (bypasses `appsettings.Development.json`), replaces both `PostsDbContext` and `AuthorsDbContext` with unique `Guid.NewGuid()` InMemory databases per factory instance, overrides `IEventStore` to InMemory, clears health check registrations, and replaces JWT bearer with `TestAuthHandler` (always authenticated). `appsettings.json` defaults all transports to `inmemory`, so no RabbitMQ or PostgreSQL is needed for tests. `AnonymousBloggingApiFactory` keeps real JWT bearer for auth-specific 401 tests. Both factories implement `IAsyncLifetime` and poll `IKnownAuthorRepository` for seeded authors before tests run (MassTransit in-memory delivers `AuthorCreatedEvent` asynchronously).

**Coverage target:** >90%. Run `dotnet test --collect:"XPlat Code Coverage"` to measure.

---

## Configuration

All switches live in `appsettings.json` (or environment-specific overrides like `appsettings.Development.json`).

| Key | Valid values | Default | Notes |
|-----|-------------|---------|-------|
| `Serialization:Format` | `json`, `xml` | `json` | Switches `IMessageSerializer` — `JsonMessageSerializer` or `XmlMessageSerializer`. No Application/API code changes needed. |
| `EventStore:Provider` | `inmemory`, `marten` | `inmemory` | Switches `IEventStore` — `InMemoryEventStore` (no deps) or `MartenEventStore` (PostgreSQL via Marten 7.x). No Application/Domain code changes needed. |
| `ReadModel:Provider` | `inmemory`, `postgresql` | `inmemory` | Switches `PostsDbContext` — EF Core InMemory (no deps) or Npgsql (PostgreSQL). Migrations applied automatically on startup via `PostsDatabaseMigrator`. |
| `MessageBus:Transport` | `inmemory`, `rabbitmq` | `inmemory` | Switches MassTransit transport. `inmemory` needs no external broker; `rabbitmq` connects via `MessageBus:RabbitMQ:Host/Username/Password`. |
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
| Shared.Domain | MediatR 12.x (IDomainEvent extends INotification) |
| Shared.Application | MediatR 12.x, FluentValidation 12.x, FluentValidation.DependencyInjectionExtensions 12.x, Microsoft.Extensions.Logging.Abstractions 8.x |
| Shared.Infrastructure | EF Core InMemory 8.x, Marten 7.x, MassTransit 8.x, MassTransit.RabbitMQ 8.x, Microsoft.Extensions.Hosting.Abstractions 8.x |
| Posts.Infrastructure | MassTransit 8.x (consumer only) |
| Posts.Application | MediatR 12.x, FluentValidation 12.x |
| Posts.Infrastructure | Npgsql.EntityFrameworkCore.PostgreSQL 8.x, AspNetCore.HealthChecks.Npgsql 8.x |
| Authors.Infrastructure | Npgsql.EntityFrameworkCore.PostgreSQL 8.x |
| BloggingSystem.Api | Swashbuckle.AspNetCore 6.x, FluentValidation 12.x, Serilog.AspNetCore 8.x, Serilog.Enrichers.Environment 3.x, Serilog.Enrichers.Thread 4.x |
| All test projects | xUnit, FluentAssertions 6.x, NSubstitute 5.x |
| Api.Tests | Microsoft.AspNetCore.Mvc.Testing 8.x |
| Architecture.Tests | NetArchTest.Rules 1.3.x |

---

## Iteration Targets

Remaining improvements, grouped by production-readiness dimension:

### Security
1. ~~**Auth**~~ ✅ JWT bearer authentication added; `POST /post` and `POST /author` require a valid token. `AnonymousBloggingApiFactory` exercises 401 paths in functional tests.
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

### Architecture Evolution — Standard Monolith → Modular Monolith ✅

~~19. **Define module public contracts**~~ ✅ 9-project modular structure in place. `Posts.*` and `Authors.*` are separate assemblies; cross-module code does not compile.  
~~20. **Schema isolation**~~ ✅ `PostsDbContext` owns schema `posts`; `AuthorsDbContext` owns schema `authors`. Shared `BloggingDbContext` removed.  
~~21. **In-process → message broker integration events**~~ ✅ `AuthorCreatedEvent` published via MassTransit (`IIntegrationEventPublisher` port). `AuthorCreatedConsumer` in Posts.Infrastructure populates local `KnownAuthors` table. Transport switches between `inmemory` (tests/default) and `rabbitmq` (docker-compose/production) via config with no code changes.  
~~22. **Per-module DI registration**~~ ✅ `AddPostsModule()`, `AddAuthorsModule()`, `AddSharedInfrastructure()` replace the old single `AddInfrastructure()`.  
~~23. **Per-module Architecture tests**~~ ✅ 23 assembly-level assertions in `LayerDependencyTests` verify no cross-module project references (includes `Authors.Contracts` boundary test).  
24. **Feature flags per module** — Once modules are self-contained, each can be toggled or deployed independently, enabling a low-risk path toward extracting a module into a microservice when load demands it.

---

## Commands

```bash
dotnet build                                         # build solution
dotnet test                                          # run all 195 tests
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
- All new Posts infrastructure implementations must register via `PostsModuleExtensions.AddPostsModule()`; Authors via `AuthorsModuleExtensions.AddAuthorsModule()`; shared via `SharedInfrastructureExtensions.AddSharedInfrastructure()`.
- New endpoints must chain `.WithName()`, `.WithTags("Posts")`, `.Produces<T>()` for Swagger completeness.
- New tests must follow the existing pattern: unit tests mock ports via NSubstitute; functional tests use `BloggingApiFactory`.
- Do not skip `dotnet test` after changes — all 195 tests must remain green.

### Module Encapsulation Rules (DDD / Hexagonal)

- **Module public API = commands + queries + response DTOs only.** These are `public sealed record` because the API layer sends them. Everything else is an implementation detail.
- **Handlers, projections, validators, notification handlers, repository implementations, DbContexts, and migrators must be `internal sealed class`.** They are wired via DI inside the module's own `Add*Module()` registration method and must never be instantiated directly from outside the module.
- **`Shared.Infrastructure` must not be referenced by module Infrastructure projects.** `Posts.Infrastructure` and `Authors.Infrastructure` own their own EF Core stack (Npgsql, migrations) — they do not share Marten or any other persistence framework imposed from above. Technology choices are per-module, not global. Only `BloggingSystem.Api` references `Shared.Infrastructure` for wiring the event-store strategy.
- **`Shared.Domain` contains ONLY `IDomainEvent` and `DomainException`.** No module-specific types (value objects, integration events, module exceptions) belong here. Apply the Golden Rule: "if uploaded as an open-source library, would it work without any business-specific knowledge?" If not, it does not belong in `Shared.Domain`.
- **Integration event contracts live in `Authors.Contracts`**, not in `Shared.Domain`. `Authors.Contracts` depends only on `Shared.Domain`. Both `Posts.*` and `Authors.*` modules may reference `Authors.Contracts`. Adding new cross-module contracts follows the same pattern.
- **Module-specific exceptions belong in the module's own `Domain`.** `KnownAuthorNotFoundException` lives in `Posts.Domain.Exceptions` because it describes a Posts-bounded-context failure (author not in local KnownAuthors cache). Never borrow exceptions across module boundaries.
- **`IDateTimeProvider` belongs in `Shared.Application`**, not `Shared.Domain`. It is an application-level infrastructure abstraction, not a business rule.
- **`InternalsVisibleTo` is the bridge for test access.** When a test project needs to construct or verify an `internal` type, add `<InternalsVisibleTo Include="<TestAssemblyName>" />` to the source project's `.csproj`. Do not make types `public` solely to satisfy tests.
- **`AddValidatorsFromAssembly` must pass `includeInternalTypes: true`** when validators are `internal`. This is already set in `Program.cs`. Do not remove this flag when adding new validators.