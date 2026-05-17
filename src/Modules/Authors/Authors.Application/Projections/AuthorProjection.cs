using Authors.Application.Ports;
using Authors.Application.ReadModels;
using Authors.Contracts;
using Shared.Domain.Events;

namespace Authors.Application.Projections;

internal sealed class AuthorProjection
{
    private readonly IAuthorReadRepository _authorRepository;

    public AuthorProjection(IAuthorReadRepository authorRepository)
    {
        _authorRepository = authorRepository;
    }

    public async Task ProjectAsync(IEnumerable<IDomainEvent> events, CancellationToken ct = default)
    {
        foreach (var @event in events)
        {
            if (@event is AuthorCreatedEvent created)
                await HandleAsync(created, ct);
        }
    }

    private Task HandleAsync(AuthorCreatedEvent @event, CancellationToken ct)
    {
        var readModel = new AuthorReadModel
        {
            Id = @event.AuthorId.Value,
            Name = @event.Name,
            Surname = @event.Surname
        };
        return _authorRepository.UpsertAsync(readModel, ct);
    }
}
