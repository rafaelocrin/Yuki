using Shared.Application.Ports;

namespace Authors.Api.Results;

public sealed class SerializedResult<T>(T value, int statusCode = 200, string? location = null) : IResult
{
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        var serializer = httpContext.RequestServices.GetRequiredService<IMessageSerializer>();

        if (location is not null)
            httpContext.Response.Headers.Location = location;

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";
        await httpContext.Response.WriteAsync(serializer.Serialize(value));
    }
}
