using Authors.Contracts;
using Posts.Domain.ValueObjects;
using Shared.Domain.Events;

namespace Posts.Domain.Events;

public sealed record PostCreatedEvent(
    Guid EventId,
    DateTime OccurredOn,
    PostId PostId,
    AuthorId AuthorId,
    string Title,
    string Description,
    string Content) : IDomainEvent;
