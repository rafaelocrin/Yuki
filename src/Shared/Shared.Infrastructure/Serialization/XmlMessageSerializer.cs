using System.Xml.Serialization;
using Shared.Application.Ports;

namespace Shared.Infrastructure.Serialization;

public sealed class XmlMessageSerializer : IMessageSerializer
{
    public string Serialize<T>(T value)
    {
        var serializer = new XmlSerializer(typeof(T));
        using var writer = new StringWriter();
        serializer.Serialize(writer, value);
        return writer.ToString();
    }

    public T? Deserialize<T>(string payload)
    {
        try
        {
            var serializer = new XmlSerializer(typeof(T));
            using var reader = new StringReader(payload);
            return (T?)serializer.Deserialize(reader);
        }
        catch
        {
            return default;
        }
    }
}
