using Authors.Contracts;
using MassTransit;
using Posts.Application.Ports;
using Posts.Application.ReadModels;

namespace Posts.Infrastructure.Consumers;

internal sealed class AuthorCreatedConsumer : IConsumer<AuthorCreatedEvent>
{
    private readonly IKnownAuthorRepository _knownAuthorRepository;

    public AuthorCreatedConsumer(IKnownAuthorRepository knownAuthorRepository)
        => _knownAuthorRepository = knownAuthorRepository;

    public Task Consume(ConsumeContext<AuthorCreatedEvent> context)
        => _knownAuthorRepository.UpsertAsync(new KnownAuthorReadModel
        {
            Id      = context.Message.AuthorId.Value,
            Name    = context.Message.Name,
            Surname = context.Message.Surname
        }, context.CancellationToken);
}
