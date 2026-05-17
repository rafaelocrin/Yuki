using Posts.Application.Commands.CreatePost;
using FluentAssertions;

namespace BloggingSystem.Application.Tests.Validators;

public sealed class CreatePostCommandValidatorTests
{
    private readonly CreatePostCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_NoErrors()
    {
        var command = new CreatePostCommand(Guid.NewGuid(), "Title", "Desc", "Content");
        var result = _validator.Validate(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyTitle_HasError()
    {
        var command = new CreatePostCommand(Guid.NewGuid(), "", "Desc", "Content");
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Title");
    }

    [Fact]
    public void Validate_WhitespaceTitle_HasError()
    {
        var command = new CreatePostCommand(Guid.NewGuid(), "   ", "Desc", "Content");
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Title");
    }

    [Fact]
    public void Validate_EmptyContent_HasError()
    {
        var command = new CreatePostCommand(Guid.NewGuid(), "Title", "Desc", "");
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Content");
    }

    [Fact]
    public void Validate_EmptyAuthorId_HasError()
    {
        var command = new CreatePostCommand(Guid.Empty, "Title", "Desc", "Content");
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "AuthorId");
    }

    [Fact]
    public void Validate_MultipleViolations_ReturnsAllErrors()
    {
        var command = new CreatePostCommand(Guid.Empty, "", "Desc", "");
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(3);
    }
}
