namespace Shared.Application.Ports;

public interface IProcessedCommandRepository
{
    Task<string?> GetResultAsync(string idempotencyKey, CancellationToken ct = default);
    Task StoreAsync(string idempotencyKey, string serializedResult, CancellationToken ct = default);
}
