using BloggingSystem.Application.Commands.CreateAuthor;
using FluentAssertions;

namespace BloggingSystem.Application.Tests.Validators;

public sealed class CreateAuthorCommandValidatorTests
{
    private readonly CreateAuthorCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_NoErrors()
    {
        var command = new CreateAuthorCommand("Alice", "Jones");
        var result = _validator.Validate(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyName_HasError()
    {
        var command = new CreateAuthorCommand("", "Jones");
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Validate_WhitespaceName_HasError()
    {
        var command = new CreateAuthorCommand("   ", "Jones");
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Validate_EmptySurname_HasError()
    {
        var command = new CreateAuthorCommand("Alice", "");
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Surname");
    }

    [Fact]
    public void Validate_BothEmpty_ReturnsBothErrors()
    {
        var command = new CreateAuthorCommand("", "");
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
    }
}
