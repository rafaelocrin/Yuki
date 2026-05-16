using BloggingSystem.Domain.Events;
using BloggingSystem.Domain.ValueObjects;
using BloggingSystem.Infrastructure.Outbox;
using BloggingSystem.Infrastructure.Persistence.ReadModel;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace BloggingSystem.Infrastructure.Tests.Outbox;

public sealed class EfCoreOutboxWriterTests : IDisposable
{
    private readonly BloggingDbContext _context;
    private readonly EfCoreOutboxWriter _writer;

    public EfCoreOutboxWriterTests()
    {
        var options = new DbContextOptionsBuilder<BloggingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new BloggingDbContext(options);
        _writer = new EfCoreOutboxWriter(_context);
    }

    public void Dispose() => _context.Dispose();

    private static PostCreatedEvent MakeEvent() =>
        new(Guid.NewGuid(), DateTime.UtcNow,
            new PostId(Guid.NewGuid()), new AuthorId(Guid.NewGuid()),
            "Title", "Desc", "Content");

    [Fact]
    public async Task WriteAsync_PersistsOneRowPerEvent()
    {
        var evt = MakeEvent();
        await _writer.WriteAsync([evt]);

        var rows = await _context.OutboxEvents.ToListAsync();
        rows.Should().HaveCount(1);
    }

    [Fact]
    public async Task WriteAsync_RowHasNullProcessedAt()
    {
        await _writer.WriteAsync([MakeEvent()]);

        var row = await _context.OutboxEvents.SingleAsync();
        row.ProcessedAt.Should().BeNull();
    }

    [Fact]
    public async Task WriteAsync_StoresVersionIndependentTypeName()
    {
        await _writer.WriteAsync([MakeEvent()]);

        var row = await _context.OutboxEvents.SingleAsync();
        row.EventType.Should().Be(
            $"{typeof(PostCreatedEvent).FullName}, {typeof(PostCreatedEvent).Assembly.GetName().Name}");
        // Must NOT contain "Version=" — ensures restorability after assembly version bumps.
        row.EventType.Should().NotContain("Version=");
    }

    [Fact]
    public async Task WriteAsync_PayloadRoundTrips()
    {
        var evt = MakeEvent();
        await _writer.WriteAsync([evt]);

        var row = await _context.OutboxEvents.SingleAsync();
        var type = Type.GetType(row.EventType)!;
        var restored = (PostCreatedEvent)System.Text.Json.JsonSerializer
            .Deserialize(row.Payload, type, EfCoreOutboxWriter.SerializerOptions)!;

        restored.PostId.Value.Should().Be(evt.PostId.Value);
        restored.AuthorId.Value.Should().Be(evt.AuthorId.Value);
        restored.Title.Should().Be(evt.Title);
    }

    [Fact]
    public async Task WriteAsync_MultipleEvents_PersistsAll()
    {
        await _writer.WriteAsync([MakeEvent(), MakeEvent(), MakeEvent()]);

        var rows = await _context.OutboxEvents.ToListAsync();
        rows.Should().HaveCount(3);
    }
}
