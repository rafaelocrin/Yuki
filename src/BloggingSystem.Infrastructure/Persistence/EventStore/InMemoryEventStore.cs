using System.Collections.Concurrent;
using BloggingSystem.Application.Ports;
using BloggingSystem.Domain.Events;

namespace BloggingSystem.Infrastructure.Persistence.EventStore;

public sealed class InMemoryEventStore : IEventStore
{
    private readonly ConcurrentDictionary<Guid, List<IDomainEvent>> _streams = new();

    public Task AppendEventsAsync(Guid streamId, IEnumerable<IDomainEvent> events, CancellationToken ct = default)
    {
        var stream = _streams.GetOrAdd(streamId, _ => new List<IDomainEvent>());
        lock (stream)
        {
            stream.AddRange(events);
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<IDomainEvent>> GetEventsAsync(Guid streamId, CancellationToken ct = default)
    {
        if (!_streams.TryGetValue(streamId, out var stream))
            return Task.FromResult<IReadOnlyList<IDomainEvent>>(Array.Empty<IDomainEvent>());

        IReadOnlyList<IDomainEvent> copy;
        lock (stream)
        {
            copy = stream.ToList().AsReadOnly();
        }
        return Task.FromResult(copy);
    }
}
