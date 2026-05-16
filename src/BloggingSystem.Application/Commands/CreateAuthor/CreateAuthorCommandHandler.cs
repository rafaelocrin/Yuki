using BloggingSystem.Application.Ports;
using BloggingSystem.Application.Projections;
using BloggingSystem.Domain.Aggregates.Author;
using MediatR;

namespace BloggingSystem.Application.Commands.CreateAuthor;

public sealed class CreateAuthorCommandHandler : IRequestHandler<CreateAuthorCommand, Guid>
{
    private readonly IEventStore _eventStore;
    private readonly AuthorProjection _projection;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateAuthorCommandHandler(
        IEventStore eventStore,
        AuthorProjection projection,
        IDateTimeProvider dateTimeProvider)
    {
        _eventStore = eventStore;
        _projection = projection;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Guid> Handle(CreateAuthorCommand command, CancellationToken cancellationToken)
    {
        var author = Author.Create(command.Name, command.Surname, _dateTimeProvider.UtcNow);

        await _eventStore.AppendEventsAsync(author.Id, author.UncommittedEvents, cancellationToken);
        await _projection.ProjectAsync(author.UncommittedEvents, cancellationToken);
        author.ClearUncommittedEvents();

        return author.Id.Value;
    }
}
