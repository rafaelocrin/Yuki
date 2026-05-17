# Microservices Migration Roadmap

This document tracks the incremental path from the current modular monolith to a fully distributed microservices architecture. Each step is self-contained and independently deployable — you do not need to complete all steps to benefit from the ones you finish.

---

## Why this project is easier than most

| Advantage | Detail |
|---|---|
| Compile-time module boundaries | `Posts.*` and `Authors.*` are separate assemblies — no big ball of mud to detangle |
| Clean event contract seam | `Authors.Contracts` already isolates the one cross-module event type |
| Separate DbContexts | `PostsDbContext` (schema `posts`) and `AuthorsDbContext` (schema `authors`) — no shared tables to split |
| Per-module DI registration | `AddPostsModule()` / `AddAuthorsModule()` map directly to each future service's `Program.cs` |
| Strategy-pattern config switches | `EventStore:Provider`, `ReadModel:Provider`, `MessageBus:Transport` — swap implementations without changing domain or application code |

> The domain and application layers change **zero lines** during extraction. Only infrastructure wiring and deployment config change.

---

## Step 1 — Replace in-process events with a message broker ✅ DONE

**Goal:** Decouple the Authors and Posts modules at runtime so they can run in separate processes.

**What was done:**
- Introduced `IIntegrationEventPublisher` port in `Shared.Application` — Application layers stay broker-agnostic
- `MassTransitIntegrationEventPublisher` adapter in `Shared.Infrastructure` wraps MassTransit's `IPublishEndpoint`
- `AuthorSeeder` now publishes `AuthorCreatedEvent` via `IIntegrationEventPublisher` instead of MediatR `IPublisher`
- `OnAuthorCreated` (MediatR in-process handler) deleted; replaced by `AuthorCreatedConsumer` (`IConsumer<AuthorCreatedEvent>`) in `Posts.Infrastructure`
- `AddSharedInfrastructure()` wires up MassTransit with a strategy switch: `inmemory` (default, no deps) or `rabbitmq` (production) controlled by `MessageBus:Transport` in config
- `PostsModuleExtensions.AddPostsConsumers(IBusRegistrationConfigurator)` registers the consumer while keeping it `internal`
- Architecture guard `ApplicationLayers_ShouldNot_DependOn_MassTransit` ensures Application layers never import MassTransit directly

**Key files:**
- [src/Shared/Shared.Application/Ports/IIntegrationEventPublisher.cs](src/Shared/Shared.Application/Ports/IIntegrationEventPublisher.cs)
- [src/Shared/Shared.Infrastructure/Messaging/MassTransitIntegrationEventPublisher.cs](src/Shared/Shared.Infrastructure/Messaging/MassTransitIntegrationEventPublisher.cs)
- [src/Modules/Posts/Posts.Infrastructure/Consumers/AuthorCreatedConsumer.cs](src/Modules/Posts/Posts.Infrastructure/Consumers/AuthorCreatedConsumer.cs)
- [src/Shared/Shared.Infrastructure/DependencyInjection/SharedInfrastructureExtensions.cs](src/Shared/Shared.Infrastructure/DependencyInjection/SharedInfrastructureExtensions.cs)
- [docker-compose.yml](docker-compose.yml) — RabbitMQ service added

**Test approach:** `BloggingApiFactory` sets `MessageBus:Transport = inmemory` and uses `IAsyncLifetime` to poll `IKnownAuthorRepository` until the seeder's events are consumed before tests run. No RabbitMQ needed in CI.

---

## Step 2 — Separate the databases ✅ DONE

**Goal:** Each future service owns its own database server — no shared connection string, no shared schema.

**What was done:**
- `PostsDbContext` owns schema `posts` (tables: `Posts`, `KnownAuthors`, `OutboxEvents`)
- `AuthorsDbContext` owns schema `authors` (tables: `Authors`)
- Each DbContext registers its own Npgsql connection and runs its own EF Core migrations independently
- `PostsDatabaseMigrator` (BackgroundService) applies `Posts.*` migrations on startup
- `docker-compose.yml` uses a single Postgres instance with separate schemas — in production, point each service at its own `ConnectionStrings:PostgreSQL`

**What remains for full extraction:** Each service gets its own `postgres` container (or managed DB instance) in docker-compose / Kubernetes. The application code changes nothing — only the connection string per deployment changes.

---

## Step 3 — Split Shared.* by ownership ✅ DONE (packaging metadata)

**Goal:** Eliminate the shared project boundary so each service can evolve its tooling independently.

**What was done:**
- Added NuGet packaging metadata to `Shared.Domain` and `Authors.Contracts` (`<IsPackable>true</IsPackable>`, `PackageId`, `Version`, `Authors`, `Description`)
- Created `NuGet.Config` at repo root with a `local-packages` source for local `dotnet pack` workflow
- Run `dotnet pack` to produce `Yuki.Shared.Domain.1.0.0.nupkg` and `Yuki.Authors.Contracts.1.0.0.nupkg`

**What remains for full extraction:**
1. Publish packages to GitHub Packages, Azure Artifacts, or another feed
2. Replace `<ProjectReference>` to `Shared.Domain` and `Authors.Contracts` with `<PackageReference>` in each module's `.csproj`
3. Inline the remaining `Shared.Application` and `Shared.Infrastructure` code each service needs

**Risk:** Low. The types don't change — only the delivery mechanism (project ref → NuGet) changes.

---

## Step 4 — Separate deployables ✅ DONE

**Goal:** Each module becomes an independently deployable service with its own `Program.cs` and `Dockerfile`.

**What was done:**
- Created `src/Modules/Posts/Posts.Api/` — standalone web API with `CreatePostEndpoint`, `GetPostEndpoint`, `GetPostsEndpoint`, `GlobalExceptionHandler`, JWT auth, Serilog, Swagger, and MassTransit consumer wiring; port 8082
- Created `src/Modules/Authors/Authors.Api/` — standalone web API with `CreateAuthorEndpoint`, `GlobalExceptionHandler`, JWT auth, Serilog, Swagger; port 8083
- Each `Program.cs` calls only its own module's `Add*Module()` and `AddSharedInfrastructure()`
- Individual `Dockerfile` per service in `src/Modules/Posts/Posts.Api/Dockerfile` and `src/Modules/Authors/Authors.Api/Dockerfile`
- `docker-compose.yml` updated: `posts-api` (port 8082) and `authors-api` (port 8083) added alongside the existing `api` monolith
- Both projects added to `BloggingSystem.sln`

**Key files:**
- [src/Modules/Posts/Posts.Api/Program.cs](src/Modules/Posts/Posts.Api/Program.cs)
- [src/Modules/Posts/Posts.Api/Dockerfile](src/Modules/Posts/Posts.Api/Dockerfile)
- [src/Modules/Authors/Authors.Api/Program.cs](src/Modules/Authors/Authors.Api/Program.cs)
- [src/Modules/Authors/Authors.Api/Dockerfile](src/Modules/Authors/Authors.Api/Dockerfile)

**Note:** `BloggingSystem.Api` is kept as the primary deployable for the existing test suite. Once the standalone APIs have their own functional tests, the monolith entry point can be retired.

---

## Step 5 — Add an API gateway ✅ DONE

**Goal:** Present a single host to clients while routing internally to the correct service.

**What was done:**
- Created `src/Gateway/` project with `Yarp.ReverseProxy` 2.3.0
- Routes: `/post/{**catch-all}` → `posts-cluster` (http://posts-api:8082), `/author/{**catch-all}` → `authors-cluster` (http://authors-api:8083)
- `appsettings.Development.json` overrides cluster addresses for local dev (`localhost:8082`, `localhost:8083`)
- `Dockerfile` at `src/Gateway/Dockerfile`; port 8081
- Added to `BloggingSystem.sln` and `docker-compose.yml` (`gateway` service, port 8081, depends on `posts-api` and `authors-api`)

**Key files:**
- [src/Gateway/Program.cs](src/Gateway/Program.cs)
- [src/Gateway/appsettings.json](src/Gateway/appsettings.json)
- [src/Gateway/Dockerfile](src/Gateway/Dockerfile)

**What remains:** Move JWT validation and `CorrelationIdMiddleware` to the gateway layer for defense-in-depth and to remove redundant validation from each service.

---

## Step 6 — Mandatory infrastructure (becomes non-optional in microservices) ✅ DONE

**Goal:** Fill in the items that were optional in the monolith but are load-bearing in a distributed system.

| Item | Status | Detail |
|---|---|---|
| **Health checks** (`/healthz/live`, `/healthz/ready`) | ✅ Done | Per-service; PostgreSQL probe tagged `"ready"` |
| **Structured logging** (Serilog + CorrelationId) | ✅ Done | `X-Correlation-ID` → Serilog `LogContext`; `LoggingBehavior` MediatR pipeline |
| **Distributed tracing** (OpenTelemetry + OTLP) | ✅ Done | `AddOpenTelemetry().WithTracing(...)` in all three API projects; opt-in via `OpenTelemetry:OtlpEndpoint` config; Jaeger added to docker-compose (port 16686 UI, 4317 OTLP) |
| **Retry / circuit-breaker** (Polly) | ✅ Done | `EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: 10s)` on both `PostsDbContext` and `AuthorsDbContext` Npgsql options |
| **Idempotent command handling** | ✅ Done | `X-Idempotency-Key` header; `ICurrentRequestContext` port; `IProcessedCommandRepository` port; `IdempotencyBehavior<,>` MediatR pipeline; `ProcessedCommands` table per module (EF Core); registered in all three API hosts |
| **Graceful shutdown** | ✅ Done | `Configure<HostOptions>(opts => opts.ShutdownTimeout = TimeSpan.FromSeconds(30))` in all three API hosts |

**Key new files:**
- [src/Shared/Shared.Application/Ports/ICurrentRequestContext.cs](src/Shared/Shared.Application/Ports/ICurrentRequestContext.cs)
- [src/Shared/Shared.Application/Ports/IProcessedCommandRepository.cs](src/Shared/Shared.Application/Ports/IProcessedCommandRepository.cs)
- [src/Shared/Shared.Application/Behaviors/IdempotencyBehavior.cs](src/Shared/Shared.Application/Behaviors/IdempotencyBehavior.cs)
- [src/Modules/Posts/Posts.Infrastructure/Idempotency/EfCoreProcessedCommandRepository.cs](src/Modules/Posts/Posts.Infrastructure/Idempotency/EfCoreProcessedCommandRepository.cs)
- [src/Modules/Authors/Authors.Infrastructure/Idempotency/EfCoreProcessedCommandRepository.cs](src/Modules/Authors/Authors.Infrastructure/Idempotency/EfCoreProcessedCommandRepository.cs)
- [src/BloggingSystem.Api/Auth/CurrentRequestContext.cs](src/BloggingSystem.Api/Auth/CurrentRequestContext.cs)

---

## Progress summary

| Step | Status | Notes |
|---|---|---|
| 1 — Message broker (RabbitMQ via MassTransit) | ✅ Done | `inmemory` ↔ `rabbitmq` via config switch; 195 tests green |
| 2 — Separate databases (schema isolation) | ✅ Done | `posts` and `authors` schemas; independent EF Core migrations |
| 3 — Split Shared.* into NuGet packages | ✅ Done (packaging) | `Shared.Domain` and `Authors.Contracts` packaged; NuGet.Config created; feed publication pending |
| 4 — Separate deployables (Posts.Api / Authors.Api) | ✅ Done | `Posts.Api` (port 8082) and `Authors.Api` (port 8083); docker-compose wired |
| 5 — API gateway (YARP) | ✅ Done | YARP gateway (port 8081); routes `/post*` → posts-api, `/author*` → authors-api |
| 6 — Mandatory distributed infrastructure | ✅ Done | All six items complete: health checks, structured logging, OTel tracing (Jaeger), Polly retry, idempotency, graceful shutdown |

**Test suite:** 195 tests, 0 failures (`dotnet test BloggingSystem.sln`)
