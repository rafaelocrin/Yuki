using BloggingSystem.Infrastructure.Serialization;
using FluentAssertions;

namespace BloggingSystem.Infrastructure.Tests.Serialization;

// XmlSerializer requires a public type with a parameterless constructor and settable properties.
public sealed class XmlTestPayload
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
}

public sealed class XmlMessageSerializerTests
{
    private readonly XmlMessageSerializer _serializer = new();

    [Fact]
    public void Serialize_ThenDeserialize_RoundTrips()
    {
        var original = new XmlTestPayload { Name = "hello", Value = 42 };
        var xml = _serializer.Serialize(original);
        var result = _serializer.Deserialize<XmlTestPayload>(xml);

        result.Should().NotBeNull();
        result!.Name.Should().Be("hello");
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Serialize_ProducesXmlFormat()
    {
        var payload = new XmlTestPayload { Name = "x", Value = 1 };
        var xml = _serializer.Serialize(payload);

        xml.Should().Contain("<");
        xml.Should().Contain(">");
        xml.Should().NotContain("{");
        xml.Should().NotContain("}");
    }

    [Fact]
    public void Serialize_ContainsPropertyValues()
    {
        var payload = new XmlTestPayload { Name = "blog", Value = 99 };
        var xml = _serializer.Serialize(payload);

        xml.Should().Contain("blog");
        xml.Should().Contain("99");
    }

    [Fact]
    public void Deserialize_InvalidXml_ReturnsNull()
    {
        var result = _serializer.Deserialize<XmlTestPayload>("not-valid-xml<<>>");
        result.Should().BeNull();
    }

    [Fact]
    public void Deserialize_EmptyString_ReturnsNull()
    {
        var result = _serializer.Deserialize<XmlTestPayload>("");
        result.Should().BeNull();
    }
}
