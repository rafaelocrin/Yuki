using MassTransit;
using Shared.Application.Ports;

namespace Shared.Infrastructure.Messaging;

internal sealed class MassTransitIntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public MassTransitIntegrationEventPublisher(IPublishEndpoint publishEndpoint)
        => _publishEndpoint = publishEndpoint;

    public Task PublishAsync<T>(T message, CancellationToken ct = default) where T : class
        => _publishEndpoint.Publish(message, ct);
}
