using BloggingSystem.Domain.ValueObjects;

namespace BloggingSystem.Domain.Events;

public sealed record AuthorCreatedEvent(
    Guid EventId,
    DateTime OccurredOn,
    AuthorId AuthorId,
    string Name,
    string Surname) : IDomainEvent;
