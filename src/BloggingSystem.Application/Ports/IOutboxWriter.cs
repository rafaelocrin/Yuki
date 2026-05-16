using BloggingSystem.Domain.Events;

namespace BloggingSystem.Application.Ports;

public interface IOutboxWriter
{
    Task WriteAsync(IEnumerable<IDomainEvent> events, CancellationToken ct = default);
}
