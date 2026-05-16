# CLAUDE.md — Agent Source of Truth

This file is loaded automatically by Claude Code at the start of every session.
It is the canonical reference for architecture decisions, patterns, and iteration targets.

---

## Project

Yuki Backend Technical Test — a blogging REST API in C# .NET 8.

**Endpoints:** `POST /post`, `GET /post/{id}?includeAuthor=true`  
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
- **Infrastructure** (`src/BloggingSystem.Infrastructure/`): `InMemoryEventStore` (singleton, thread-safe), EF Core InMemory `BloggingDbContext`, `JsonMessageSerializer`, `DataSeeder` (seeds 2 authors via `IHostedService`), `InfrastructureServiceExtensions.AddInfrastructure()`.
- **API** (`src/BloggingSystem.Api/`): Minimal APIs, Swashbuckle Swagger at `/swagger`. `public partial class Program {}` required for `WebApplicationFactory`.

**Hexagonal boundary rule:** Domain and Application never reference Infrastructure or API namespaces. Infrastructure implements Application ports. API wires everything via DI.

**Event Sourcing flow:** `CreatePostCommand` → `Post.Create()` raises `PostCreatedEvent` → append to `IEventStore` → `PostProjection.ProjectAsync()` upserts EF Core read model synchronously.

**Serialization Strategy pattern:** `IMessageSerializer` (Application port) — currently `JsonMessageSerializer`. Adding XML support = implement `XmlMessageSerializer`, register in DI. No Application or API code changes needed.

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

---

## Tests

102 tests, 0 failures. Run with `dotnet test`.

| Project | Count | Type |
|---------|-------|------|
| `BloggingSystem.Domain.Tests` | 26 | Pure unit |
| `BloggingSystem.Application.Tests` | 23 | Unit (NSubstitute mocks + concrete validators) |
| `BloggingSystem.Infrastructure.Tests` | 24 | Integration (real EF InMemory) |
| `BloggingSystem.Api.Tests` | 23 | Functional (WebApplicationFactory) |
| `BloggingSystem.Architecture.Tests` | 7 | Architecture (NetArchTest.Rules) |

**Test isolation:** `BloggingApiFactory` replaces the DbContext with a unique `Guid.NewGuid()` InMemory database per factory instance to prevent cross-test contamination.

**Coverage target:** >90%. Run `dotnet test --collect:"XPlat Code Coverage"` to measure.

---

## NuGet Packages

| Project | Packages |
|---------|----------|
| Application | MediatR 12.x |
| Infrastructure | EF Core InMemory 8.x, Microsoft.Extensions.Hosting.Abstractions 8.x |
| API | Swashbuckle.AspNetCore 6.x |
| All test projects | xUnit, FluentAssertions 6.x, NSubstitute 5.x |
| Api.Tests | Microsoft.AspNetCore.Mvc.Testing 8.x |
| Architecture.Tests | NetArchTest.Rules 1.3.x |

---

## Iteration Targets

Things to improve in future sessions, in priority order:

1. **XML serializer** — implement `XmlMessageSerializer : IMessageSerializer` in Infrastructure; add a config switch (e.g., `appsettings.json` key) to select format at startup.
2. **`POST /author` endpoint** — currently authors are seeded at startup; a real create-author flow would complete the domain model.
3. **Persistent event store** — replace `InMemoryEventStore` with a real store (Marten/PostgreSQL or EventStoreDB) behind the same `IEventStore` port — no Application/Domain changes required.
4. **Persistent read model** — replace EF Core InMemory with SQL Server or PostgreSQL; add EF Core migrations.
5. **Pagination** — `GET /post` list endpoint with cursor or page-based pagination.
6. **Validation middleware** — centralise validation errors into RFC 7807 `ProblemDetails` responses instead of `{ error: "..." }` anonymous objects.
7. **Auth** — add JWT bearer authentication; scope `POST /post` to authenticated authors only.
8. **OpenAPI XML comments** — enable `<GenerateDocumentationFile>true</GenerateDocumentationFile>` in the API project and wire XML comments into `AddSwaggerGen` for richer Swagger descriptions.

---

## Commands

```bash
dotnet build                                         # build solution
dotnet test                                          # run all 102 tests
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
- Do not skip `dotnet test` after changes — all 102 tests must remain green.