using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Ports;
using System.Text.Json;

namespace Shared.Application.Behaviors;

/// <summary>
/// Short-circuits duplicate requests when X-Idempotency-Key header is present and the key
/// was already processed. Returns the cached serialized result without re-executing the handler.
/// No-op when ICurrentRequestContext or IProcessedCommandRepository are not registered (e.g. tests).
/// </summary>
public sealed class IdempotencyBehavior<TRequest, TResponse>(IServiceProvider sp)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var key = sp.GetService<ICurrentRequestContext>()?.IdempotencyKey;
        if (key is null)
            return await next(ct);

        var repo = sp.GetService<IProcessedCommandRepository>();
        if (repo is null)
            return await next(ct);

        var cached = await repo.GetResultAsync(key, ct);
        if (cached is not null)
            return JsonSerializer.Deserialize<TResponse>(cached)!;

        var result = await next(ct);
        await repo.StoreAsync(key, JsonSerializer.Serialize(result), ct);
        return result;
    }
}
