namespace Posts.Application.ReadModels;

public sealed class KnownAuthorReadModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Surname { get; set; } = string.Empty;
}
