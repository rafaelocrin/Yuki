using System.Text.Json;
using Posts.Infrastructure.Persistence;
using Shared.Application.Ports;
using Shared.Domain.Events;

namespace Posts.Infrastructure.Outbox;

internal sealed class EfCoreOutboxWriter : IOutboxWriter
{
    public static string GetEventTypeName(Type type) =>
        $"{type.FullName}, {type.Assembly.GetName().Name}";

    public static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly PostsDbContext _context;

    public EfCoreOutboxWriter(PostsDbContext context)
    {
        _context = context;
    }

    public async Task WriteAsync(IEnumerable<IDomainEvent> events, CancellationToken ct = default)
    {
        foreach (var @event in events)
        {
            _context.OutboxEvents.Add(new OutboxEvent
            {
                Id = Guid.NewGuid(),
                EventType = GetEventTypeName(@event.GetType()),
                Payload = JsonSerializer.Serialize(@event, @event.GetType(), SerializerOptions),
                OccurredOn = @event.OccurredOn
            });
        }
        await _context.SaveChangesAsync(ct);
    }
}
