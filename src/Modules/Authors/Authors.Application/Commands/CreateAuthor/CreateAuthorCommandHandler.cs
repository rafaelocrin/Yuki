using Authors.Application.Projections;
using Authors.Contracts;
using Authors.Domain.Aggregates;
using MediatR;
using Shared.Application.Ports;

namespace Authors.Application.Commands.CreateAuthor;

internal sealed class CreateAuthorCommandHandler : IRequestHandler<CreateAuthorCommand, Guid>
{
    private readonly IEventStore _eventStore;
    private readonly AuthorProjection _projection;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IPublisher _publisher;

    public CreateAuthorCommandHandler(
        IEventStore eventStore,
        AuthorProjection projection,
        IDateTimeProvider dateTimeProvider,
        IPublisher publisher)
    {
        _eventStore = eventStore;
        _projection = projection;
        _dateTimeProvider = dateTimeProvider;
        _publisher = publisher;
    }

    public async Task<Guid> Handle(CreateAuthorCommand command, CancellationToken cancellationToken)
    {
        var author = Author.Create(command.Name, command.Surname, _dateTimeProvider.UtcNow);

        await _eventStore.AppendEventsAsync(author.Id, author.UncommittedEvents, cancellationToken);
        await _projection.ProjectAsync(author.UncommittedEvents, cancellationToken);

        // Publish so cross-module handlers (OnAuthorCreated in Posts) can react
        foreach (var e in author.UncommittedEvents.OfType<AuthorCreatedEvent>())
            await _publisher.Publish(e, cancellationToken);

        author.ClearUncommittedEvents();

        return author.Id.Value;
    }
}
