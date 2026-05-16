# Yuki Blogging System

A RESTful blogging API built with .NET 8, implementing Hexagonal Architecture, CQRS, and Event Sourcing.

## Architecture Overview

```
┌────────────────────────────────────────────────────────────────┐
│  API Layer  (BloggingSystem.Api)                               │
│  Minimal APIs — POST /post, GET /post/{id}, POST /author       │
└──────────────────────┬─────────────────────────────────────────┘
                       │ IMediator
┌──────────────────────▼─────────────────────────────────────────┐
│  Application Layer  (BloggingSystem.Application)               │
│  CQRS: CreatePostCommand, CreateAuthorCommand,                 │
│        GetPostByIdQuery                                        │
│  Ports: IEventStore, IPostReadRepository,                      │
│         IAuthorReadRepository, IMessageSerializer              │
│  Projections: PostProjection, AuthorProjection                 │
└──────────┬──────────────────────────────────────────┬──────────┘
           │                                          │
┌──────────▼──────────┐                   ┌──────────▼──────────┐
│  Domain Layer        │                   │ Infrastructure      │
│  (BloggingSystem     │                   │ (BloggingSystem     │
│   .Domain)           │                   │  .Infrastructure)   │
│                      │                   │                     │
│  Aggregates:         │                   │ InMemoryEventStore  │
│   Post, Author       │                   │ MartenEventStore *  │
│  Domain Events       │                   │ EF Core (InMemory)  │
│  Value Objects       │                   │ JsonSerializer      │
│                      │                   │ XmlSerializer *     │
│                      │                   │ DataSeeder          │
└──────────────────────┘                   └─────────────────────┘
                                           * config-switchable
```

**Hexagonal boundaries**: Domain has zero infrastructure dependencies. Application defines ports (interfaces); Infrastructure implements them as adapters.

**Event Sourcing**: `CreatePostCommand` raises `PostCreatedEvent`, which is persisted to the event store and synchronously projected onto the EF Core read model.

**CQRS**: Write side (commands) goes through the event store; read side (queries) hits the EF Core read model projection.

**Serialization Strategy**: `IMessageSerializer` (Application port) decouples the format. Switch between `JsonMessageSerializer` and `XmlMessageSerializer` via `appsettings.json` — no Application or API code changes needed.

**Event Store Strategy**: `IEventStore` (Application port) decouples the persistence backend. Switch between `InMemoryEventStore` (default, no deps) and `MartenEventStore` (PostgreSQL via Marten 7.x) via `appsettings.json` — no Application or Domain code changes needed.

## Project Structure

```
src/
  BloggingSystem.Domain/          # Pure domain logic — no deps
  BloggingSystem.Application/     # CQRS handlers, ports, projections
  BloggingSystem.Infrastructure/  # EF Core, event store, JSON serializer
  BloggingSystem.Api/             # Minimal API entry point

tests/
  BloggingSystem.Domain.Tests/         # Unit tests
  BloggingSystem.Application.Tests/    # Unit tests (mocked ports)
  BloggingSystem.Infrastructure.Tests/ # Integration tests
  BloggingSystem.Api.Tests/            # Functional tests (WebApplicationFactory)
```

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- Docker Desktop *(optional, for containerised run)*
- PostgreSQL *(optional — only required when `EventStore:Provider` is `marten`)*

## Running Locally

```bash
cd src/BloggingSystem.Api
dotnet run
```

API is available at `http://localhost:5000` (or check console output for the port).

**Swagger UI:** `http://localhost:5002/swagger`

## Running with Docker

```bash
docker compose up --build
```

API is available at `http://localhost:8080`.

**Swagger UI:** `http://localhost:8080/swagger`

## Running Tests

```bash
dotnet test
```

To collect code coverage:

```bash
dotnet test --collect:"XPlat Code Coverage"
```

Coverage reports are written to `tests/<project>/TestResults/`. To generate an HTML report:

```bash
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"tests/**/TestResults/**/coverage.cobertura.xml" -targetdir:coveragereport -reporttypes:Html
```

Then open `coveragereport/index.html`.

## Seeded Authors

Two authors are created automatically on startup. Use their IDs when calling `POST /post`:

| Name        | ID                                     |
|-------------|----------------------------------------|
| Jane Doe    | `11111111-1111-1111-1111-111111111111` |
| John Smith  | `22222222-2222-2222-2222-222222222222` |

## Configuration

All switches live in `appsettings.json` (or environment-specific overrides):

| Key | Valid values | Default | Notes |
|-----|-------------|---------|-------|
| `Serialization:Format` | `json`, `xml` | `json` | Switches between `JsonMessageSerializer` and `XmlMessageSerializer`. |
| `EventStore:Provider` | `inmemory`, `marten` | `inmemory` | Switches between `InMemoryEventStore` and `MartenEventStore` (PostgreSQL). |
| `ConnectionStrings:PostgreSQL` | Npgsql connection string | *(none)* | **Required** when `EventStore:Provider` is `marten`. |

**Example — switch to XML serialization + PostgreSQL event store:**

```json
{
  "Serialization": { "Format": "xml" },
  "EventStore": { "Provider": "marten" },
  "ConnectionStrings": {
    "PostgreSQL": "Host=localhost;Port=5432;Database=blogging;Username=postgres;Password=postgres"
  }
}
```

## API Reference

### POST /author

Creates a new author.

**Request body:**

```json
{
  "name": "Alice",
  "surname": "Smith"
}
```

**Responses:**

| Status | Description |
|--------|-------------|
| 201    | Created — `{ "id": "<guid>" }`, `Location: /author/<id>` |
| 400    | Validation error (empty name or surname) |

### POST /post

Creates a new blog post.

**Request body:**

```json
{
  "authorId": "11111111-1111-1111-1111-111111111111",
  "title": "My First Post",
  "description": "A short summary",
  "content": "The full body of the post."
}
```

**Responses:**

| Status | Description                    |
|--------|--------------------------------|
| 201    | Created — `{ "id": "<guid>" }`, `Location: /post/<id>` |
| 400    | Validation error               |
| 404    | Author not found               |

### GET /post/{id}

Retrieves a post by ID.

**Query parameters:**

| Parameter      | Type    | Default | Description                         |
|----------------|---------|---------|-------------------------------------|
| `includeAuthor`| boolean | `false` | Include author details in response  |

**Responses:**

| Status | Description        |
|--------|--------------------|
| 200    | Post DTO (see below)|
| 400    | Invalid GUID format |
| 404    | Post not found     |

**Example response (`includeAuthor=true`):**

```json
{
  "id": "a3f1c2d4-...",
  "authorId": "11111111-1111-1111-1111-111111111111",
  "title": "My First Post",
  "description": "A short summary",
  "content": "The full body of the post.",
  "author": {
    "id": "11111111-1111-1111-1111-111111111111",
    "name": "Jane",
    "surname": "Doe"
  }
}
```

## Sample cURL Requests

```bash
# Create an author
curl -X POST http://localhost:8080/author \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Alice",
    "surname": "Smith"
  }'

# Create a post (using a seeded author ID)
curl -X POST http://localhost:8080/post \
  -H "Content-Type: application/json" \
  -d '{
    "authorId": "11111111-1111-1111-1111-111111111111",
    "title": "Hello World",
    "description": "My first post",
    "content": "This is the body of my post."
  }'

# Get a post
curl http://localhost:8080/post/<POST_ID>

# Get a post with author details
curl "http://localhost:8080/post/<POST_ID>?includeAuthor=true"
```
