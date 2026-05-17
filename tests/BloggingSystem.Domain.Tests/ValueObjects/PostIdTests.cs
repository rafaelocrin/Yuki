using Posts.Domain.ValueObjects;
using FluentAssertions;

namespace BloggingSystem.Domain.Tests.ValueObjects;

public sealed class PostIdTests
{
    [Fact]
    public void Constructor_WithValidGuid_SetsValue()
    {
        var guid = Guid.NewGuid();
        var postId = new PostId(guid);
        postId.Value.Should().Be(guid);
    }

    [Fact]
    public void Constructor_WithEmptyGuid_Throws()
    {
        var act = () => new PostId(Guid.Empty);
        act.Should().Throw<ArgumentException>().WithMessage("*PostId*");
    }

    [Fact]
    public void ImplicitConversion_ToGuid_Works()
    {
        var guid = Guid.NewGuid();
        var postId = new PostId(guid);
        Guid result = postId;
        result.Should().Be(guid);
    }

    [Fact]
    public void ImplicitConversion_FromGuid_Works()
    {
        var guid = Guid.NewGuid();
        PostId postId = guid;
        postId.Value.Should().Be(guid);
    }

    [Fact]
    public void ToString_ReturnsGuidString()
    {
        var guid = Guid.NewGuid();
        var postId = new PostId(guid);
        postId.ToString().Should().Be(guid.ToString());
    }

    [Fact]
    public void Equality_TwoPostIdsWithSameGuid_AreEqual()
    {
        var guid = Guid.NewGuid();
        var a = new PostId(guid);
        var b = new PostId(guid);
        a.Should().Be(b);
    }
}
