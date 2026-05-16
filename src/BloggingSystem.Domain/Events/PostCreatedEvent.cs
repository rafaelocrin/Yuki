using BloggingSystem.Domain.ValueObjects;

namespace BloggingSystem.Domain.Events;

public sealed record PostCreatedEvent(
    Guid EventId,
    DateTime OccurredOn,
    PostId PostId,
    AuthorId AuthorId,
    string Title,
    string Description,
    string Content) : IDomainEvent;
