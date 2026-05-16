using BloggingSystem.Domain.Events;

namespace BloggingSystem.Application.Ports;

public interface IEventStore
{
    Task AppendEventsAsync(Guid streamId, IEnumerable<IDomainEvent> events, CancellationToken ct = default);
    Task<IReadOnlyList<IDomainEvent>> GetEventsAsync(Guid streamId, CancellationToken ct = default);
}
