namespace BloggingSystem.Application.ReadModels;

public sealed class PostReadModel
{
    public Guid Id { get; set; }
    public Guid AuthorId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public AuthorReadModel? Author { get; set; }
}
