namespace BloggingSystem.Application.DTOs;

public sealed record CreatePostRequest(
    Guid AuthorId,
    string Title,
    string Description,
    string Content);
