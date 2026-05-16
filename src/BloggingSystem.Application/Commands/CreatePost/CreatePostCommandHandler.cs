using BloggingSystem.Application.Ports;
using BloggingSystem.Application.Projections;
using BloggingSystem.Domain.Aggregates.Post;
using BloggingSystem.Domain.Exceptions;
using BloggingSystem.Domain.ValueObjects;
using MediatR;

namespace BloggingSystem.Application.Commands.CreatePost;

public sealed class CreatePostCommandHandler : IRequestHandler<CreatePostCommand, Guid>
{
    private readonly IEventStore _eventStore;
    private readonly IAuthorReadRepository _authorRepository;
    private readonly IOutboxWriter _outboxWriter;
    private readonly PostProjection _projection;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreatePostCommandHandler(
        IEventStore eventStore,
        IAuthorReadRepository authorRepository,
        IOutboxWriter outboxWriter,
        PostProjection projection,
        IDateTimeProvider dateTimeProvider)
    {
        _eventStore = eventStore;
        _authorRepository = authorRepository;
        _outboxWriter = outboxWriter;
        _projection = projection;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Guid> Handle(CreatePostCommand command, CancellationToken cancellationToken)
    {
        var author = await _authorRepository.GetByIdAsync(command.AuthorId, cancellationToken);
        if (author is null)
            throw new AuthorNotFoundException(new AuthorId(command.AuthorId));

        var post = Post.Create(
            new AuthorId(command.AuthorId),
            command.Title,
            command.Description,
            command.Content,
            _dateTimeProvider.UtcNow);

        await _eventStore.AppendEventsAsync(post.Id, post.UncommittedEvents, cancellationToken);

        // Write to outbox BEFORE inline projection.  If the process crashes after this
        // line, the OutboxProcessor will re-apply the projection on restart, guaranteeing
        // the read model never silently drifts from the event log.
        await _outboxWriter.WriteAsync(post.UncommittedEvents, cancellationToken);

        await _projection.ProjectAsync(post.UncommittedEvents, cancellationToken);
        post.ClearUncommittedEvents();

        return post.Id.Value;
    }
}
