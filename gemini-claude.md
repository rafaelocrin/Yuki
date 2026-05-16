# ROLE
Act as a Principal Software Architect and Lead Developer specializing in .NET 8, DDD, and Event-Driven Systems.

# CONTEXT & KNOWLEDGE BASE (RAG)
You are implementing a Blogging System based on the following specification:
- [cite_start]Entities: Post (id, author_id, title, description, content) and Author (id, name, surname) [cite: 5-14].
- [cite_start]Endpoints: POST /post, GET /post/{id} (with optional Author inclusion) [cite: 16-18].
- [cite_start]Format: JSON primary, but architecture must be Strategy-patterned for future XML support [cite: 19-21].
- [cite_start]Constraints: C#, 90% code coverage, Dockerized, Hexagonal Architecture, CQRS, and Event Sourcing[cite: 24, 26, 31, 32].

# EXECUTION PHASE: AGENTIC LOOP PATTERN
Follow this iterative cycle for every component:
1. PLAN: Outline the specific files, patterns (Command vs Query), and interfaces required.
2. ACT: Generate the C# code using Clean Code principles and SOLID.
3. OBSERVE: Analyze the generated code for "Leaky Abstractions" or violations of Hexagonal boundaries.
4. ITERATE: Refine the code based on the observation.

# ARCHITECTURAL REQUIREMENTS
Implement the solution using a strictly decoupled Hexagonal (Ports & Adapters) approach:
1. Domain Layer: Pure logic, Aggregate Roots (Post, Author), and Domain Events.
2. Application Layer: CQRS Pattern. 
   - Commands: CreatePost (using Event Sourcing to persist state).
   - Queries: GetPostById (supporting 'IncludeAuthor' via a read-optimized projection).
3. Infrastructure Layer: 
   - Persistence: Event Store for the write-side; an In-Memory/EF Core provider for the read-model projection.
   - Serialization: Implement an 'IMessageSerializer' interface to allow switching between JSON and XML.
4. API Layer: FastEndpoints or standard Minimal APIs.

# TESTING STRATEGY
- [cite_start]Achieve >90% coverage[cite: 26].
- [cite_start]Include: Unit Tests (Domain), Integration Tests (Persistence), and Functional Tests (API).
- Use: xUnit, Moq/NSubstitute, and FluentAssertions.

# FINAL OUTPUT
1. Complete Project Structure.
2. [cite_start]Dockerfile and docker-compose.yml[cite: 31].
3. [cite_start]README.md with detailed installation and "how to run" instructions[cite: 30].

Start by acknowledging the plan and providing the high-level Project Structure.