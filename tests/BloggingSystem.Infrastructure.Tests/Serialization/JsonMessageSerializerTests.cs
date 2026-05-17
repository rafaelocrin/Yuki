using Shared.Infrastructure.Serialization;
using FluentAssertions;

namespace BloggingSystem.Infrastructure.Tests.Serialization;

[Trait("Category", "Integration")]
public sealed class JsonMessageSerializerTests
{
    private readonly JsonMessageSerializer _serializer = new();

    private sealed record TestPayload(string Name, int Value);

    [Fact]
    public void Serialize_ThenDeserialize_RoundTrips()
    {
        var original = new TestPayload("hello", 42);
        var json = _serializer.Serialize(original);
        var result = _serializer.Deserialize<TestPayload>(json);

        result.Should().NotBeNull();
        result!.Name.Should().Be("hello");
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Serialize_ProducesJsonFormat()
    {
        var payload = new TestPayload("x", 1);
        var json = _serializer.Serialize(payload);

        json.Should().Contain("{");
        json.Should().Contain("}");
        json.Should().NotContain("<");
        json.Should().NotContain("<?xml");
    }

    [Fact]
    public void Serialize_UsesCamelCasePropertyNames()
    {
        var payload = new TestPayload("x", 1);
        var json = _serializer.Serialize(payload);

        json.Should().Contain("\"name\"");
        json.Should().Contain("\"value\"");
    }

    [Fact]
    public void Deserialize_InvalidJson_ReturnsNull()
    {
        var result = _serializer.Deserialize<TestPayload>("not-valid-json{{");
        result.Should().BeNull();
    }

    [Fact]
    public void Deserialize_EmptyString_ReturnsNull()
    {
        var result = _serializer.Deserialize<TestPayload>("");
        result.Should().BeNull();
    }
}
