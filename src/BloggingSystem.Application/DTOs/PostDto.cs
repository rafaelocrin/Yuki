namespace BloggingSystem.Application.DTOs;

public sealed record PostDto(
    Guid Id,
    Guid AuthorId,
    string Title,
    string Description,
    string Content,
    AuthorDto? Author);
