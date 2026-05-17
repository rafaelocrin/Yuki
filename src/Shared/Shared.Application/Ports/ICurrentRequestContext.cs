namespace Shared.Application.Ports;

public interface ICurrentRequestContext
{
    string? IdempotencyKey { get; }
}
