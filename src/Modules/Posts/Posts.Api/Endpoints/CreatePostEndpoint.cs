using MediatR;
using Posts.Api.Extensions;
using Posts.Api.Models;
using Posts.Api.Results;
using Posts.Application.Commands.CreatePost;
using Posts.Application.DTOs;

namespace Posts.Api.Endpoints;

public static class CreatePostEndpoint
{
    public static void MapCreatePostEndpoint(this WebApplication app)
    {
        app.MapPost("/post", HandleAsync)
            .WithName("CreatePost")
            .WithTags("Posts")
            .WithSummary("Create a new blog post")
            .WithDescription(
                "Creates a new blog post. Use one of the seeded author IDs: " +
                "`11111111-1111-1111-1111-111111111111` (Jane Doe) or " +
                "`22222222-2222-2222-2222-222222222222` (John Smith).")
            .Accepts<CreatePostRequest>("application/json")
            .Produces<CreatePostResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(HttpRequest request, IMediator mediator)
    {
        var dto = await request.DeserializeBodyAsync<CreatePostRequest>();
        if (dto is null)
            return ResultExtensions.BadRequest("Invalid request body.");

        var command = new CreatePostCommand(dto.AuthorId, dto.Title, dto.Description, dto.Content);
        var id = await mediator.Send(command);
        return ResultExtensions.Created(new CreatePostResponse(id), $"/post/{id}");
    }
}
