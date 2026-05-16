namespace BloggingSystem.Application.ReadModels;

public sealed class AuthorReadModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Surname { get; set; } = string.Empty;
}
