using Authors.Contracts;
using MediatR;
using Posts.Application.Ports;
using Posts.Application.Projections;
using Posts.Domain.Aggregates;
using Posts.Domain.Exceptions;
using Shared.Application.Ports;

namespace Posts.Application.Commands.CreatePost;

internal sealed class CreatePostCommandHandler : IRequestHandler<CreatePostCommand, Guid>
{
    private readonly IEventStore _eventStore;
    private readonly IKnownAuthorRepository _knownAuthorRepository;
    private readonly IOutboxWriter _outboxWriter;
    private readonly PostProjection _projection;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreatePostCommandHandler(
        IEventStore eventStore,
        IKnownAuthorRepository knownAuthorRepository,
        IOutboxWriter outboxWriter,
        PostProjection projection,
        IDateTimeProvider dateTimeProvider)
    {
        _eventStore = eventStore;
        _knownAuthorRepository = knownAuthorRepository;
        _outboxWriter = outboxWriter;
        _projection = projection;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Guid> Handle(CreatePostCommand command, CancellationToken cancellationToken)
    {
        var knownAuthor = await _knownAuthorRepository.GetByIdAsync(command.AuthorId, cancellationToken);
        if (knownAuthor is null)
            throw new KnownAuthorNotFoundException(command.AuthorId);

        var post = Post.Create(
            new AuthorId(command.AuthorId),
            command.Title,
            command.Description,
            command.Content,
            _dateTimeProvider.UtcNow);

        await _eventStore.AppendEventsAsync(post.Id, post.UncommittedEvents, cancellationToken);

        await _outboxWriter.WriteAsync(post.UncommittedEvents, cancellationToken);

        await _projection.ProjectAsync(post.UncommittedEvents, cancellationToken);
        post.ClearUncommittedEvents();

        return post.Id.Value;
    }
}
