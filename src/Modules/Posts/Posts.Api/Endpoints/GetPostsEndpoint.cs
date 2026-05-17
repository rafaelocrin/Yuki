using MediatR;
using Posts.Api.Models;
using Posts.Api.Results;
using Posts.Application.DTOs;
using Posts.Application.Queries.GetPosts;
using Shared.Application.DTOs;

namespace Posts.Api.Endpoints;

public static class GetPostsEndpoint
{
    public static void MapGetPostsEndpoint(this WebApplication app)
    {
        app.MapGet("/post", HandleAsync)
            .WithName("GetPosts")
            .WithTags("Posts")
            .WithSummary("Get a paginated list of posts")
            .WithDescription("Returns a page of posts ordered by creation date. Use `page` and `pageSize` to navigate. Pass `includeAuthor=true` to embed author details.")
            .Produces<PostsPagedResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> HandleAsync(
        IMediator mediator,
        int page = 1,
        int pageSize = 10,
        bool includeAuthor = false)
    {
        var query = new GetPostsQuery(page, pageSize, includeAuthor);
        var result = await mediator.Send(query);
        return ResultExtensions.Ok(Map(result));
    }

    private static PostsPagedResponse Map(PagedResult<PostDto> result) => new(
        result.Items.Select(p => new PostResponse(
            p.Id,
            p.AuthorId,
            p.Title,
            p.Description,
            p.Content,
            p.Author is null ? null : new AuthorResponse(p.Author.Id, p.Author.Name, p.Author.Surname)
        )).ToList(),
        result.TotalCount,
        result.Page,
        result.PageSize,
        result.TotalPages);
}
