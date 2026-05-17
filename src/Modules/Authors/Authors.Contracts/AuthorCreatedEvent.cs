using Shared.Domain.Events;

namespace Authors.Contracts;

public sealed record AuthorCreatedEvent(
    Guid EventId,
    DateTime OccurredOn,
    AuthorId AuthorId,
    string Name,
    string Surname) : IDomainEvent;
