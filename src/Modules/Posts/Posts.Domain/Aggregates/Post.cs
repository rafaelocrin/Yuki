using Authors.Contracts;
using Posts.Domain.Events;
using Posts.Domain.ValueObjects;
using Shared.Domain.Events;
using Shared.Domain.Exceptions;

namespace Posts.Domain.Aggregates;

public sealed class Post
{
    private readonly List<IDomainEvent> _uncommittedEvents = new();

    public PostId Id { get; private set; } = null!;
    public AuthorId AuthorId { get; private set; } = null!;
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;

    public IReadOnlyList<IDomainEvent> UncommittedEvents => _uncommittedEvents.AsReadOnly();

    private Post() { }

    public static Post Create(AuthorId authorId, string title, string description, string content, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Title cannot be empty.");
        if (string.IsNullOrWhiteSpace(content))
            throw new DomainException("Content cannot be empty.");

        var post = new Post();
        post.RaiseEvent(new PostCreatedEvent(
            EventId:     Guid.NewGuid(),
            OccurredOn:  now,
            PostId:      new PostId(Guid.NewGuid()),
            AuthorId:    authorId,
            Title:       title,
            Description: description ?? string.Empty,
            Content:     content));
        return post;
    }

    public static Post Rehydrate(IEnumerable<IDomainEvent> events)
    {
        var post = new Post();
        foreach (var @event in events)
        {
            if (@event is PostCreatedEvent created)
                post.Apply(created);
        }
        return post;
    }

    public void ClearUncommittedEvents() => _uncommittedEvents.Clear();

    private void RaiseEvent(PostCreatedEvent @event)
    {
        Apply(@event);
        _uncommittedEvents.Add(@event);
    }

    private void Apply(PostCreatedEvent @event)
    {
        Id = @event.PostId;
        AuthorId = @event.AuthorId;
        Title = @event.Title;
        Description = @event.Description;
        Content = @event.Content;
    }
}
