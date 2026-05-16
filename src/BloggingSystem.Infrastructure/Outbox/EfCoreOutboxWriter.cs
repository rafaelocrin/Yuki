using System.Text.Json;
using BloggingSystem.Application.Ports;
using BloggingSystem.Domain.Events;
using BloggingSystem.Infrastructure.Persistence.ReadModel;

namespace BloggingSystem.Infrastructure.Outbox;

public sealed class EfCoreOutboxWriter : IOutboxWriter
{
    // Version-independent type name: "Namespace.Type, AssemblyName"
    public static string GetEventTypeName(Type type) =>
        $"{type.FullName}, {type.Assembly.GetName().Name}";

    public static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly BloggingDbContext _context;

    public EfCoreOutboxWriter(BloggingDbContext context)
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
