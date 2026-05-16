using BloggingSystem.Application.Ports;

namespace BloggingSystem.Infrastructure.Time;

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
