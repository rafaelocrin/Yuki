using BloggingSystem.Application.Ports;
using BloggingSystem.Application.ReadModels;
using BloggingSystem.Domain.Events;

namespace BloggingSystem.Application.Projections;

public sealed class PostProjection
{
    private readonly IPostReadRepository _postRepository;

    public PostProjection(IPostReadRepository postRepository)
    {
        _postRepository = postRepository;
    }

    public async Task ProjectAsync(IEnumerable<IDomainEvent> events, CancellationToken ct = default)
    {
        foreach (var @event in events)
        {
            if (@event is PostCreatedEvent created)
                await HandleAsync(created, ct);
        }
    }

    private Task HandleAsync(PostCreatedEvent @event, CancellationToken ct)
    {
        var readModel = new PostReadModel
        {
            Id = @event.PostId,
            AuthorId = @event.AuthorId,
            Title = @event.Title,
            Description = @event.Description,
            Content = @event.Content,
            CreatedAt = @event.OccurredOn
        };
        return _postRepository.UpsertAsync(readModel, ct);
    }
}
