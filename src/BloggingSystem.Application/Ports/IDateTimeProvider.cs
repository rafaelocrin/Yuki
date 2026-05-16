namespace BloggingSystem.Application.Ports;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
