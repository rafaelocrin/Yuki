using Shared.Application.Behaviors;
using FluentAssertions;
using FluentValidation;
using MediatR;

namespace BloggingSystem.Application.Tests.Behaviors;

public record TestRequest(string Value) : IRequest<string>;

public sealed class AlwaysPassValidator : AbstractValidator<TestRequest> { }

public sealed class RequiredValueValidator : AbstractValidator<TestRequest>
{
    public RequiredValueValidator()
    {
        RuleFor(x => x.Value).NotEmpty().WithMessage("Value is required.");
    }
}

public sealed class ValidationBehaviorTests
{
    private static RequestHandlerDelegate<string> NextReturning(string value) =>
        _ => Task.FromResult(value);

    [Fact]
    public async Task Handle_WhenNoValidatorsRegistered_CallsNext()
    {
        var behavior = new ValidationBehavior<TestRequest, string>([]);
        var result = await behavior.Handle(new TestRequest("x"), NextReturning("ok"), CancellationToken.None);
        result.Should().Be("ok");
    }

    [Fact]
    public async Task Handle_WhenValidatorPasses_CallsNext()
    {
        var behavior = new ValidationBehavior<TestRequest, string>([new AlwaysPassValidator()]);
        var result = await behavior.Handle(new TestRequest("x"), NextReturning("ok"), CancellationToken.None);
        result.Should().Be("ok");
    }

    [Fact]
    public async Task Handle_WhenValidatorFails_ThrowsValidationException()
    {
        var behavior = new ValidationBehavior<TestRequest, string>([new RequiredValueValidator()]);
        var act = async () => await behavior.Handle(new TestRequest(""), NextReturning("ok"), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>()
            .Where(ex => ex.Errors.Any(e => e.PropertyName == "Value"));
    }

    [Fact]
    public async Task Handle_WhenValidatorFails_DoesNotCallNext()
    {
        var nextCalled = false;
        RequestHandlerDelegate<string> next = _ => { nextCalled = true; return Task.FromResult("ok"); };

        var behavior = new ValidationBehavior<TestRequest, string>([new RequiredValueValidator()]);
        try { await behavior.Handle(new TestRequest(""), next, CancellationToken.None); } catch { }

        nextCalled.Should().BeFalse();
    }
}
