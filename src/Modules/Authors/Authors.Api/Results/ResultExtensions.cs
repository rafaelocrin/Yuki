using HttpResults = Microsoft.AspNetCore.Http.Results;

namespace Authors.Api.Results;

internal static class ResultExtensions
{
    internal static IResult Ok<T>(T value)
        => new SerializedResult<T>(value);

    internal static IResult Created<T>(T value, string location)
        => new SerializedResult<T>(value, StatusCodes.Status201Created, location);

    internal static IResult BadRequest(string detail)
        => HttpResults.Problem(detail: detail, statusCode: StatusCodes.Status400BadRequest);
}
