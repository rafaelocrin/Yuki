using Shared.Domain.Events;

namespace Shared.Application.Ports;

public interface IOutboxWriter
{
    Task WriteAsync(IEnumerable<IDomainEvent> events, CancellationToken ct = default);
}
