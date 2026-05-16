using System.Text.Json;
using BloggingSystem.Application.Ports;
using BloggingSystem.Application.Projections;
using BloggingSystem.Application.ReadModels;
using BloggingSystem.Domain.Events;
using BloggingSystem.Domain.ValueObjects;
using BloggingSystem.Infrastructure.Outbox;
using BloggingSystem.Infrastructure.Persistence.ReadModel;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace BloggingSystem.Infrastructure.Tests.Outbox;

public sealed class OutboxProcessorTests : IDisposable
{
    private readonly BloggingDbContext _context;
    private readonly IPostReadRepository _postRepo;
    private readonly OutboxProcessor _processor;

    public OutboxProcessorTests()
    {
        var dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<BloggingDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        _context = new BloggingDbContext(options);
        _postRepo = Substitute.For<IPostReadRepository>();

        // Build a scope factory that returns the pre-built context and a real projection.
        var services = new ServiceCollection();
        services.AddSingleton(_context);
        services.AddSingleton<IPostReadRepository>(_ => _postRepo);
        services.AddSingleton<PostProjection>();

        var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        _processor = new OutboxProcessor(scopeFactory, NullLogger<OutboxProcessor>.Instance);
    }

    public void Dispose() => _context.Dispose();

    private OutboxEvent PendingEntry(IDomainEvent evt) => new()
    {
        Id = Guid.NewGuid(),
        EventType = EfCoreOutboxWriter.GetEventTypeName(evt.GetType()),
        Payload = JsonSerializer.Serialize(evt, evt.GetType(), EfCoreOutboxWriter.SerializerOptions),
        OccurredOn = evt.OccurredOn
    };

    // ── ProcessPendingAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task ProcessPendingAsync_NoPendingEntries_DoesNotCallProjection()
    {
        await _processor.ProcessPendingAsync();

        await _postRepo.DidNotReceive().UpsertAsync(Arg.Any<PostReadModel>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessPendingAsync_PendingEntry_CallsProjection()
    {
        var evt = new PostCreatedEvent(Guid.NewGuid(), DateTime.UtcNow,
            new PostId(Guid.NewGuid()), new AuthorId(Guid.NewGuid()),
            "Title", "Desc", "Body");

        _context.OutboxEvents.Add(PendingEntry(evt));
        await _context.SaveChangesAsync();

        await _processor.ProcessPendingAsync();

        await _postRepo.Received(1).UpsertAsync(Arg.Any<PostReadModel>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessPendingAsync_PendingEntry_MarksEntryAsProcessed()
    {
        var evt = new PostCreatedEvent(Guid.NewGuid(), DateTime.UtcNow,
            new PostId(Guid.NewGuid()), new AuthorId(Guid.NewGuid()),
            "Title", "Desc", "Body");

        var entry = PendingEntry(evt);
        _context.OutboxEvents.Add(entry);
        await _context.SaveChangesAsync();

        await _processor.ProcessPendingAsync();

        var stored = await _context.OutboxEvents.FindAsync(entry.Id);
        stored!.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ProcessPendingAsync_AlreadyProcessedEntry_IsNotProjectedAgain()
    {
        var evt = new PostCreatedEvent(Guid.NewGuid(), DateTime.UtcNow,
            new PostId(Guid.NewGuid()), new AuthorId(Guid.NewGuid()),
            "Title", "Desc", "Body");

        var entry = PendingEntry(evt);
        entry.ProcessedAt = DateTime.UtcNow.AddMinutes(-1);  // already processed
        _context.OutboxEvents.Add(entry);
        await _context.SaveChangesAsync();

        await _processor.ProcessPendingAsync();

        await _postRepo.DidNotReceive().UpsertAsync(Arg.Any<PostReadModel>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessPendingAsync_MultipleEntries_ProjectsAllAndMarksProcessed()
    {
        for (var i = 0; i < 3; i++)
        {
            var evt = new PostCreatedEvent(Guid.NewGuid(), DateTime.UtcNow,
                new PostId(Guid.NewGuid()), new AuthorId(Guid.NewGuid()),
                $"Title{i}", "Desc", "Body");
            _context.OutboxEvents.Add(PendingEntry(evt));
        }
        await _context.SaveChangesAsync();

        await _processor.ProcessPendingAsync();

        await _postRepo.Received(3).UpsertAsync(Arg.Any<PostReadModel>(), Arg.Any<CancellationToken>());
        var unprocessed = await _context.OutboxEvents.CountAsync(e => e.ProcessedAt == null);
        unprocessed.Should().Be(0);
    }
}
