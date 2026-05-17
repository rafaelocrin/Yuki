using Authors.Contracts;
using FluentAssertions;

namespace BloggingSystem.Domain.Tests.ValueObjects;

public sealed class AuthorIdTests
{
    [Fact]
    public void Constructor_WithValidGuid_SetsValue()
    {
        var guid = Guid.NewGuid();
        var authorId = new AuthorId(guid);
        authorId.Value.Should().Be(guid);
    }

    [Fact]
    public void Constructor_WithEmptyGuid_Throws()
    {
        var act = () => new AuthorId(Guid.Empty);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ImplicitConversion_ToGuid_Works()
    {
        var guid = Guid.NewGuid();
        var authorId = new AuthorId(guid);
        Guid result = authorId;
        result.Should().Be(guid);
    }

    [Fact]
    public void ImplicitConversion_FromGuid_Works()
    {
        var guid = Guid.NewGuid();
        AuthorId authorId = guid;
        authorId.Value.Should().Be(guid);
    }
}
