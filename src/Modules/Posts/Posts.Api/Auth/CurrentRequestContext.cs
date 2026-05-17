using Shared.Application.Ports;

namespace Posts.Api.Auth;

internal sealed class CurrentRequestContext(IHttpContextAccessor httpContextAccessor) : ICurrentRequestContext
{
    public string? IdempotencyKey =>
        httpContextAccessor.HttpContext?.Request.Headers["X-Idempotency-Key"].FirstOrDefault();
}
