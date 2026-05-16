using BloggingSystem.Application.Ports;

namespace BloggingSystem.Api.Extensions;

internal static class HttpRequestExtensions
{
    internal static async Task<T?> DeserializeBodyAsync<T>(this HttpRequest request)
    {
        var serializer = request.HttpContext.RequestServices.GetRequiredService<IMessageSerializer>();
        using var reader = new StreamReader(request.Body);
        return serializer.Deserialize<T>(await reader.ReadToEndAsync());
    }
}
