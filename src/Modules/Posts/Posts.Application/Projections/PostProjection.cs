using Posts.Application.Ports;
using Posts.Application.ReadModels;
using Posts.Domain.Events;
using Shared.Domain.Events;

namespace Posts.Application.Projections;

internal sealed class PostProjection
{
    private readonly IPostReadRepository _postRepository;
    private readonly IKnownAuthorRepository _knownAuthorRepository;

    public PostProjection(IPostReadRepository postRepository, IKnownAuthorRepository knownAuthorRepository)
    {
        _postRepository = postRepository;
        _knownAuthorRepository = knownAuthorRepository;
    }

    public async Task ProjectAsync(IEnumerable<IDomainEvent> events, CancellationToken ct = default)
    {
        foreach (var @event in events)
        {
            if (@event is PostCreatedEvent created)
                await HandleAsync(created, ct);
        }
    }

    private async Task HandleAsync(PostCreatedEvent @event, CancellationToken ct)
    {
        var author = await _knownAuthorRepository.GetByIdAsync(@event.AuthorId, ct);
        var readModel = new PostReadModel
        {
            Id = @event.PostId,
            AuthorId = @event.AuthorId,
            AuthorName = author?.Name ?? string.Empty,
            AuthorSurname = author?.Surname ?? string.Empty,
            Title = @event.Title,
            Description = @event.Description,
            Content = @event.Content,
            CreatedAt = @event.OccurredOn
        };
        await _postRepository.UpsertAsync(readModel, ct);
    }
}
