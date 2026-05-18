# Improvement Backlog

Pending improvements for the Yuki Blogging System, organised by production-readiness dimension with a prioritised implementation order.

---

## Priority Assessment

### Tier 1 — Must have before any production traffic

**1. Observability: Health checks + Structured logging** ✅
The single highest-leverage pair. Without them you are completely blind in production — no way to detect failures, diagnose errors, or even know if the pod is healthy enough to receive traffic. Health checks take ~2 hours; Serilog structured logging half a day. They unlock the ability to debug everything else on this list.

**2. Auth** ✅
`POST /post` and `POST /author` are completely open. Any anonymous client can write data. This is the first thing any security review flags and is a blocker for production deployment.

**3. Outbox pattern** ✅
There is a real correctness bug right now: if the process crashes between `AppendEventsAsync` and `ProjectAsync`, the event store has the event but the read model does not. The read model silently drifts from the event log. This is not hypothetical — any restart loses in-flight projections. Fix this before you have real users.

---

### Tier 2 — High return, moderate effort

**4. Retry / circuit-breaker**
PostgreSQL is the single external dependency for both stores. A transient connection error (pool exhaustion, network blip) currently propagates directly as a 500. Polly adds 2–3 lines of configuration and eliminates most of this class of failures.

**5. Modular monolith**
The current `Posts` and `Authors` concerns share a `BloggingDbContext`, share `InfrastructureServiceExtensions`, and there is nothing enforcing the boundary at compile time. The longer you wait, the more entangled these become. Doing this now while the codebase is small is a half-day refactor; doing it at 10× the size is a multi-week migration.

---

### Tier 3 — Important but can follow

~~**6. Idempotent command handling**~~ ✅ `X-Idempotency-Key` header + `ProcessedCommands` table + `IdempotencyBehavior<,>` MediatR pipeline. `POST /post` and `POST /author` are safe to retry without duplicate data.

**7. Secret management** — Urgent only when the repository is shared outside the team or deployed to a shared environment.

**8. Async projection worker / caching** — Performance optimisations that are irrelevant until you have measurable load.

---

### Suggested implementation order

| Step | Item | Estimated effort | Status |
|------|------|-----------------|--------|
| 1 | Health checks + Structured logging | 1 day | ✅ Done |
| 2 | Auth (JWT bearer) | 2–3 days | ✅ Done |
| 3 | Outbox pattern | 1–2 days | ✅ Done |
| 4 | Retry / circuit-breaker | half a day | ✅ Done |
| 5 | Modular monolith | 2–3 days | ✅ Done |

The first three together turn this from a well-engineered prototype into something that can run in production under scrutiny.

---

## Full Backlog

### Security

1. ~~**Auth**~~ ✅ JWT bearer authentication added; `POST /post` and `POST /author` require a valid token. `AnonymousBloggingApiFactory` exercises 401 paths in functional tests.
2. **Input sanitisation** — Strip or reject HTML/script content in `title`, `description`, and `content` fields to prevent stored-XSS if output is ever rendered in a browser.
3. **Rate limiting** — Apply ASP.NET Core's built-in rate limiter (`AddRateLimiter`) to write endpoints to prevent abuse.
4. **Secret management** — Move the `ConnectionStrings:PostgreSQL` value out of `appsettings.json` into environment variables or a secrets manager (Azure Key Vault, AWS Secrets Manager) before shipping to production.

### Observability

5. ~~**Structured logging**~~ ✅ Serilog structured logging with `CorrelationId` enrichment via `X-Correlation-ID` header and `LoggingBehavior` MediatR pipeline. Config-driven minimum levels.
6. ~~**Distributed tracing**~~ ✅ `OpenTelemetry.Extensions.Hosting` with OTLP exporter wired in all 3 API hosts. Spans visible in Jaeger at `localhost:16686`; conditional on `OpenTelemetry:OtlpEndpoint` config.
7. ~~**Health checks**~~ ✅ `/healthz/live` (liveness, no checks) and `/healthz/ready` (readiness, PostgreSQL probe tagged "ready") exposed with JSON response writer.
8. **Metrics** — Expose Prometheus-compatible metrics (`/metrics`) via `prometheus-net.AspNetCore`; track request latency, command throughput, and projection lag.
9. **OpenAPI XML comments** — Enable `<GenerateDocumentationFile>true</GenerateDocumentationFile>` in the API project and wire XML docs into `AddSwaggerGen` for richer Swagger descriptions.

### Resilience

10. ~~**Retry / circuit-breaker**~~ ✅ `EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: 10s)` applied to both `PostsDbContext` and `AuthorsDbContext` via Npgsql's built-in Polly integration.
11. ~~**Idempotent command handling**~~ ✅ `X-Idempotency-Key` header; `ProcessedCommands` table; `IdempotencyBehavior<,>` MediatR pipeline enforces once-only execution for `POST /post` and `POST /author`.
12. ~~**Outbox pattern**~~ ✅ `OutboxEvent` table + `EfCoreOutboxWriter` + `OutboxProcessor` (BackgroundService). Events written to outbox before inline projection; on restart the processor re-projects any pending entries, closing the crash-between-append-and-project window.

### Scalability & Performance

13. **Async projection worker** — Decouple `PostProjection` from the write path; have a background worker consume events from Marten's async daemon so the command handler returns faster.
14. **Read-model caching** — Add a Redis-backed response cache for `GET /post/{id}` and `GET /post`; invalidate on `PostCreatedEvent`.
15. **`GET /author/{id}` query** — Add a read endpoint for authors so consumers do not need to embed author data in every post fetch.
16. **Cursor-based pagination** — Upgrade `GET /post` from page-based to keyset/cursor pagination (`?after=<createdAt>&limit=20`) for consistent performance at large offsets.

### Availability

17. ~~**Graceful shutdown**~~ ✅ `hostOptions.ShutdownTimeout = 30s` configured in all 3 API hosts (`BloggingSystem.Api`, `Posts.Api`, `Authors.Api`); all hosted services honour `CancellationToken`.
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

19. ~~**Define module public contracts**~~ ✅ 10-project modular structure in place. `Posts.*` and `Authors.*` are separate assemblies; cross-module code does not compile.
20. ~~**Schema isolation**~~ ✅ `PostsDbContext` owns schema `posts`; `AuthorsDbContext` owns schema `authors`. Shared `BloggingDbContext` removed.
21. ~~**In-process → message broker integration events**~~ ✅ `AuthorCreatedEvent` published via MassTransit (`IIntegrationEventPublisher` port). `AuthorCreatedConsumer` (MassTransit `IConsumer<AuthorCreatedEvent>`) in `Posts.Infrastructure` populates local `KnownAuthors` table. Transport switches between `inmemory` (tests/default) and `rabbitmq` (docker-compose/production) via config.
22. ~~**Per-module DI registration**~~ ✅ `AddPostsModule()`, `AddAuthorsModule()`, `AddSharedInfrastructure()` replace the old single `AddInfrastructure()`.
23. ~~**Per-module Architecture tests**~~ ✅ 23 assembly-level assertions in `LayerDependencyTests` verify no cross-module project references (includes `Authors.Contracts` boundary test).
24. **Feature flags per module** — Once modules are self-contained, each can be toggled or deployed independently, enabling a low-risk path toward extracting a module into a microservice when load demands it.

> **Why this matters across all other dimensions:** Schema isolation removes shared-table write contention that limits scalability; module boundaries make it safe to apply different retry/cache policies per domain; independent `DbContext` per module allows schema migrations to be scoped and rolled back without affecting other modules.
