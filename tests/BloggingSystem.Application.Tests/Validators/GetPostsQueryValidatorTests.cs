using BloggingSystem.Application.Queries.GetPosts;
using FluentAssertions;

namespace BloggingSystem.Application.Tests.Validators;

public sealed class GetPostsQueryValidatorTests
{
    private readonly GetPostsQueryValidator _validator = new();

    [Fact]
    public void Validate_ValidQuery_NoErrors()
    {
        var result = _validator.Validate(new GetPostsQuery(1, 10, false));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_PageZero_HasError()
    {
        var result = _validator.Validate(new GetPostsQuery(0, 10, false));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetPostsQuery.Page));
    }

    [Fact]
    public void Validate_PageSizeZero_HasError()
    {
        var result = _validator.Validate(new GetPostsQuery(1, 0, false));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetPostsQuery.PageSize));
    }

    [Fact]
    public void Validate_PageSizeOver100_HasError()
    {
        var result = _validator.Validate(new GetPostsQuery(1, 101, false));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetPostsQuery.PageSize));
    }

    [Fact]
    public void Validate_PageSizeAt100_NoErrors()
    {
        var result = _validator.Validate(new GetPostsQuery(1, 100, false));
        result.IsValid.Should().BeTrue();
    }
}
