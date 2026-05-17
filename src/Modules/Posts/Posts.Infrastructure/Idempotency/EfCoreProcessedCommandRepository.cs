using Microsoft.EntityFrameworkCore;
using Posts.Infrastructure.Persistence;
using Shared.Application.Ports;

namespace Posts.Infrastructure.Idempotency;

internal sealed class EfCoreProcessedCommandRepository(PostsDbContext db) : IProcessedCommandRepository
{
    public async Task<string?> GetResultAsync(string idempotencyKey, CancellationToken ct = default)
    {
        var entry = await db.ProcessedCommands
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.IdempotencyKey == idempotencyKey, ct);
        return entry?.SerializedResult;
    }

    public async Task StoreAsync(string idempotencyKey, string serializedResult, CancellationToken ct = default)
    {
        db.ProcessedCommands.Add(new ProcessedCommand
        {
            IdempotencyKey   = idempotencyKey,
            SerializedResult = serializedResult,
            CreatedAt        = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);
    }
}
