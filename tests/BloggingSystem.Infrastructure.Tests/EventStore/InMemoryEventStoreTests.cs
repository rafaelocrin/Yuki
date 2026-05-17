using Posts.Domain.Events;
using Shared.Domain.Events;
using Shared.Infrastructure.EventStore;
using FluentAssertions;
using NSubstitute;

namespace BloggingSystem.Infrastructure.Tests.EventStore;

[Trait("Category", "Integration")]
public sealed class InMemoryEventStoreTests
{
    private readonly InMemoryEventStore _store = new();

    [Fact]
    public async Task AppendAndGetEvents_ReturnsCorrectEvents()
    {
        var streamId = Guid.NewGuid();
        var evt = new PostCreatedEvent(Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(), Guid.NewGuid(),
            "T", "D", "C");

        await _store.AppendEventsAsync(streamId, new[] { evt });
        var result = await _store.GetEventsAsync(streamId);

        result.Should().HaveCount(1);
        result[0].Should().BeEquivalentTo(evt);
    }

    [Fact]
    public async Task GetEvents_ForUnknownStream_ReturnsEmpty()
    {
        var result = await _store.GetEventsAsync(Guid.NewGuid());
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task AppendEvents_MultipleTimes_AccumulatesEvents()
    {
        var streamId = Guid.NewGuid();
        var evt1 = new PostCreatedEvent(Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(), Guid.NewGuid(), "T1", "", "C1");
        var evt2 = new PostCreatedEvent(Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(), Guid.NewGuid(), "T2", "", "C2");

        await _store.AppendEventsAsync(streamId, new[] { evt1 });
        await _store.AppendEventsAsync(streamId, new[] { evt2 });

        var result = await _store.GetEventsAsync(streamId);
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task AppendEvents_DifferentStreams_AreIsolated()
    {
        var stream1 = Guid.NewGuid();
        var stream2 = Guid.NewGuid();
        var evt = new PostCreatedEvent(Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(), Guid.NewGuid(), "T", "", "C");

        await _store.AppendEventsAsync(stream1, new[] { evt });

        var result = await _store.GetEventsAsync(stream2);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task AppendEvents_ConcurrentWrites_DoesNotLoseEvents()
    {
        var streamId = Guid.NewGuid();
        var tasks = Enumerable.Range(0, 20).Select(i =>
        {
            var e = new PostCreatedEvent(Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(), Guid.NewGuid(),
                $"T{i}", "", $"C{i}");
            return _store.AppendEventsAsync(streamId, new[] { e });
        });

        await Task.WhenAll(tasks);

        var result = await _store.GetEventsAsync(streamId);
        result.Should().HaveCount(20);
    }
}
