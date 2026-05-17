using Authors.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Application.Ports;

namespace Authors.Infrastructure.Idempotency;

internal sealed class EfCoreProcessedCommandRepository(AuthorsDbContext db) : IProcessedCommandRepository
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
