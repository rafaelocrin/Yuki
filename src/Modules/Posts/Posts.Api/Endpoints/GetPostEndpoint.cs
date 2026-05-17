using MediatR;
using Posts.Api.Models;
using Posts.Api.Results;
using Posts.Application.DTOs;
using Posts.Application.Queries.GetPostById;
using Posts.Domain.ValueObjects;

namespace Posts.Api.Endpoints;

public static class GetPostEndpoint
{
    public static void MapGetPostEndpoint(this WebApplication app)
    {
        app.MapGet("/post/{id}", HandleAsync)
            .WithName("GetPostById")
            .WithTags("Posts")
            .WithSummary("Get a post by ID")
            .WithDescription("Retrieves a blog post by its ID. Pass `includeAuthor=true` to include the author's details in the response.")
            .Produces<PostResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(string id, IMediator mediator, bool includeAuthor = false)
    {
        if (!Guid.TryParse(id, out var postGuid) || postGuid == Guid.Empty)
            return ResultExtensions.BadRequest("Invalid post id format.");

        var query = new GetPostByIdQuery(new PostId(postGuid), includeAuthor);
        var post = await mediator.Send(query);
        return ResultExtensions.Ok(Map(post));
    }

    private static PostResponse Map(PostDto dto) => new(
        dto.Id,
        dto.AuthorId,
        dto.Title,
        dto.Description,
        dto.Content,
        dto.Author is null ? null : new AuthorResponse(dto.Author.Id, dto.Author.Name, dto.Author.Surname));
}
