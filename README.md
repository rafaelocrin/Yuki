# Yuki Blogging System

A RESTful blogging API built with .NET 8, implementing Hexagonal Architecture, CQRS, and Event Sourcing.

## Architecture Overview

```
┌────────────────────────────────────────────────────────────────┐
│  API Layer  (BloggingSystem.Api)                               │
│  Minimal APIs — POST /post, GET /post/{id}                     │
└──────────────────────┬─────────────────────────────────────────┘
                       │ IMediator
┌──────────────────────▼─────────────────────────────────────────┐
│  Application Layer  (BloggingSystem.Application)               │
│  CQRS: CreatePostCommand, GetPostByIdQuery                     │
│  Ports: IEventStore, IPostReadRepository,                      │
│         IAuthorReadRepository, IMessageSerializer              │
│  PostProjection: translates events → read model               │
└──────────┬──────────────────────────────────────────┬──────────┘
           │                                          │
┌──────────▼──────────┐                   ┌──────────▼──────────┐
│  Domain Layer        │                   │ Infrastructure      │
│  (BloggingSystem     │                   │ (BloggingSystem     │
│   .Domain)           │                   │  .Infrastructure)   │
│                      │                   │                     │
│  Aggregates:         │                   │ InMemoryEventStore  │
│   Post, Author       │                   │ EF Core (InMemory)  │
│  Domain Events       │                   │ JsonSerializer      │
│  Value Objects       │                   │ DataSeeder          │
└──────────────────────┘                   └─────────────────────┘
```

**Hexagonal boundaries**: Domain has zero infrastructure dependencies. Application defines ports (interfaces); Infrastructure implements them as adapters.

**Event Sourcing**: `CreatePostCommand` raises `PostCreatedEvent`, which is persisted to the event store and synchronously projected onto the EF Core read model.

**CQRS**: Write side (commands) goes through the event store; read side (queries) hits the EF Core read model projection.

**Serialization Strategy**: `IMessageSerializer` (Application port) decouples the format. `JsonMessageSerializer` is the current implementation — swap in `XmlMessageSerializer` without touching the API or Application layers.

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

## API Reference

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
# Create a post
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
