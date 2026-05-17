using Authors.Application.Projections;
using Authors.Contracts;
using Authors.Domain.Aggregates;
using MediatR;
using Shared.Application.Ports;
using Shared.Domain.Events;

namespace Authors.Application.Commands.CreateAuthor;

internal sealed class CreateAuthorCommandHandler : IRequestHandler<CreateAuthorCommand, Guid>
{
    private readonly IEventStore _eventStore;
    private readonly AuthorProjection _projection;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IIntegrationEventPublisher _integrationEventPublisher;

    public CreateAuthorCommandHandler(
        IEventStore eventStore,
        AuthorProjection projection,
        IDateTimeProvider dateTimeProvider,
        IIntegrationEventPublisher integrationEventPublisher)
    {
        _eventStore = eventStore;
        _projection = projection;
        _dateTimeProvider = dateTimeProvider;
        _integrationEventPublisher = integrationEventPublisher;
    }

    public async Task<Guid> Handle(CreateAuthorCommand command, CancellationToken cancellationToken)
    {
        var author = Author.Create(command.Name, command.Surname, _dateTimeProvider.UtcNow);

        await _eventStore.AppendEventsAsync(author.Id, author.UncommittedEvents, cancellationToken);
        await _projection.ProjectAsync(author.UncommittedEvents, cancellationToken);

        foreach (var e in author.UncommittedEvents.OfType<AuthorCreatedEvent>())
            await _integrationEventPublisher.PublishAsync(e, cancellationToken);

        author.ClearUncommittedEvents();

        return author.Id.Value;
    }
}
