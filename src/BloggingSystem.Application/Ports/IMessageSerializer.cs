namespace BloggingSystem.Application.Ports;

public interface IMessageSerializer
{
    string Serialize<T>(T value);
    T? Deserialize<T>(string payload);
}
