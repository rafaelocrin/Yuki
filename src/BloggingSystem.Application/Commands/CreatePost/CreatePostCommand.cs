using MediatR;

namespace BloggingSystem.Application.Commands.CreatePost;

public sealed record CreatePostCommand(
    Guid AuthorId,
    string Title,
    string Description,
    string Content) : IRequest<Guid>;
