using Authors.Contracts;
using MediatR;
using Posts.Application.Ports;
using Posts.Application.ReadModels;

namespace Posts.Application.Handlers;

internal sealed class OnAuthorCreated : INotificationHandler<AuthorCreatedEvent>
{
    private readonly IKnownAuthorRepository _knownAuthorRepository;

    public OnAuthorCreated(IKnownAuthorRepository knownAuthorRepository)
    {
        _knownAuthorRepository = knownAuthorRepository;
    }

    public async Task Handle(AuthorCreatedEvent notification, CancellationToken cancellationToken)
    {
        await _knownAuthorRepository.UpsertAsync(new KnownAuthorReadModel
        {
            Id = notification.AuthorId.Value,
            Name = notification.Name,
            Surname = notification.Surname
        }, cancellationToken);
    }
}
