using BloggingSystem.Application.Ports;
using BloggingSystem.Application.ReadModels;
using BloggingSystem.Domain.Events;

namespace BloggingSystem.Application.Projections;

public sealed class AuthorProjection
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
