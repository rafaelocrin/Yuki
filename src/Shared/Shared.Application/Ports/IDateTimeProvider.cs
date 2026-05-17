namespace Shared.Application.Ports;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
