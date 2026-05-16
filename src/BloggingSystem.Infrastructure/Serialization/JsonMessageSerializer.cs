using System.Text.Json;
using BloggingSystem.Application.Ports;

namespace BloggingSystem.Infrastructure.Serialization;

public sealed class JsonMessageSerializer : IMessageSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);

    public T? Deserialize<T>(string payload)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(payload, Options);
        }
        catch (JsonException)
        {
            return default;
        }
    }
}
