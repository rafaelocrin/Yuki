namespace BloggingSystem.Api.Models;

public sealed record PostResponse(
    Guid Id,
    Guid AuthorId,
    string Title,
    string Description,
    string Content,
    AuthorResponse? Author);
