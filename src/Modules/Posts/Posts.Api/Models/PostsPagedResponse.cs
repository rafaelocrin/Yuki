namespace Posts.Api.Models;

public sealed record PostsPagedResponse(
    IReadOnlyList<PostResponse> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);
