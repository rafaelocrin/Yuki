using System.Text.Json;
using Authors.Contracts;
using Posts.Application.Ports;
using Posts.Application.Projections;
using Posts.Application.ReadModels;
using Posts.Domain.Events;
using Posts.Domain.ValueObjects;
using Posts.Infrastructure.Outbox;
using Posts.Infrastructure.Persistence;
using Shared.Domain.Events;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace BloggingSystem.Infrastructure.Tests.Outbox;

[Trait("Category", "Integration")]
public sealed class OutboxProcessorTests : IDisposable
{
    private readonly PostsDbContext _context;
    private readonly IPostReadRepository _postRepo;
    private readonly IKnownAuthorRepository _knownAuthorRepo;
    private readonly OutboxProcessor _processor;

    public OutboxProcessorTests()
    {
        var dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<PostsDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        _context = new PostsDbContext(options);
        _postRepo = Substitute.For<IPostReadRepository>();
        _knownAuthorRepo = Substitute.For<IKnownAuthorRepository>();

        var services = new ServiceCollection();
        services.AddSingleton(_context);
        services.AddSingleton<IPostReadRepository>(_ => _postRepo);
        services.AddSingleton<IKnownAuthorRepository>(_ => _knownAuthorRepo);
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
        entry.ProcessedAt = DateTime.UtcNow.AddMinutes(-1);
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
