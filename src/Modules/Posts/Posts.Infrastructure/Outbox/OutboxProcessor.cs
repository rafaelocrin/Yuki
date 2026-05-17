using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Posts.Application.Projections;
using Posts.Infrastructure.Persistence;
using Shared.Domain.Events;

namespace Posts.Infrastructure.Outbox;

internal sealed class OutboxProcessor : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessor> _logger;

    public OutboxProcessor(IServiceScopeFactory scopeFactory, ILogger<OutboxProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OutboxProcessor failed during processing cycle.");
            }

            try
            {
                await Task.Delay(PollingInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    public async Task ProcessPendingAsync(CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PostsDbContext>();
        var projection = scope.ServiceProvider.GetRequiredService<PostProjection>();

        var pending = await db.OutboxEvents
            .Where(e => e.ProcessedAt == null)
            .OrderBy(e => e.OccurredOn)
            .Take(100)
            .ToListAsync(ct);

        if (pending.Count == 0) return;

        _logger.LogInformation("OutboxProcessor dispatching {Count} pending outbox entries.", pending.Count);

        var events = new List<IDomainEvent>(pending.Count);
        foreach (var entry in pending)
        {
            var type = Type.GetType(entry.EventType);
            if (type is null)
            {
                _logger.LogWarning(
                    "Unknown event type '{EventType}' in outbox entry {Id}; skipping.",
                    entry.EventType, entry.Id);
                continue;
            }

            var ev = (IDomainEvent)JsonSerializer.Deserialize(entry.Payload, type,
                EfCoreOutboxWriter.SerializerOptions)!;
            events.Add(ev);
        }

        if (events.Count > 0)
            await projection.ProjectAsync(events, ct);

        var now = DateTime.UtcNow;
        foreach (var entry in pending)
            entry.ProcessedAt = now;

        await db.SaveChangesAsync(ct);

        _logger.LogInformation("OutboxProcessor marked {Count} outbox entries as processed.", pending.Count);
    }
}
