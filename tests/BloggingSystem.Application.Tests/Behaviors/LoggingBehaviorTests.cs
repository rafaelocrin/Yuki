using Shared.Application.Behaviors;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace BloggingSystem.Application.Tests.Behaviors;

public sealed class LoggingBehaviorTests
{
    private readonly ILogger<LoggingBehavior<TestRequest, string>> _logger =
        Substitute.For<ILogger<LoggingBehavior<TestRequest, string>>>();

    [Fact]
    public async Task Handle_CallsNextAndReturnsResult()
    {
        var behavior = new LoggingBehavior<TestRequest, string>(_logger);
        RequestHandlerDelegate<string> next = _ => Task.FromResult("result");

        var result = await behavior.Handle(new TestRequest("x"), next, CancellationToken.None);

        result.Should().Be("result");
    }

    [Fact]
    public async Task Handle_WhenNextThrows_RethrowsException()
    {
        var behavior = new LoggingBehavior<TestRequest, string>(_logger);
        RequestHandlerDelegate<string> next = _ => throw new InvalidOperationException("boom");

        var act = async () => await behavior.Handle(new TestRequest("x"), next, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");
    }
}
