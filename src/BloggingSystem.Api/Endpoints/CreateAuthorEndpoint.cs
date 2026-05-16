using BloggingSystem.Api.Extensions;
using BloggingSystem.Api.Models;
using BloggingSystem.Api.Results;
using BloggingSystem.Application.Commands.CreateAuthor;
using BloggingSystem.Application.DTOs;
using MediatR;

namespace BloggingSystem.Api.Endpoints;

public static class CreateAuthorEndpoint
{
    public static void MapCreateAuthorEndpoint(this WebApplication app)
    {
        app.MapPost("/author", HandleAsync)
            .WithName("CreateAuthor")
            .WithTags("Authors")
            .WithSummary("Create a new author")
            .WithDescription("Creates a new author with a name and surname.")
            .Accepts<CreateAuthorRequest>("application/json")
            .Produces<CreateAuthorResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(HttpRequest request, IMediator mediator)
    {
        var dto = await request.DeserializeBodyAsync<CreateAuthorRequest>();
        if (dto is null)
            return ResultExtensions.BadRequest("Invalid request body.");

        var command = new CreateAuthorCommand(dto.Name, dto.Surname);
        var id = await mediator.Send(command);
        return ResultExtensions.Created(new CreateAuthorResponse(id), $"/author/{id}");
    }
}
