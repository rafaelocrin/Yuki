using BloggingSystem.Application.Ports;
using BloggingSystem.Domain.Events;
using Marten;

namespace BloggingSystem.Infrastructure.Persistence.EventStore;

public sealed class MartenEventStore : IEventStore
{
    private readonly IDocumentStore _store;

    public MartenEventStore(IDocumentStore store)
    {
        _store = store;
    }

    public async Task AppendEventsAsync(Guid streamId, IEnumerable<IDomainEvent> events, CancellationToken ct = default)
    {
        await using var session = _store.LightweightSession();
        session.Events.Append(streamId, events.OfType<object>().ToArray());
        await session.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<IDomainEvent>> GetEventsAsync(Guid streamId, CancellationToken ct = default)
    {
        await using var session = _store.LightweightSession();
        var stream = await session.Events.FetchStreamAsync(streamId, token: ct);
        return stream.Select(e => (IDomainEvent)e.Data).ToList().AsReadOnly();
    }
}
