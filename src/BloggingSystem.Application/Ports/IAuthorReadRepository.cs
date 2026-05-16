using BloggingSystem.Application.ReadModels;

namespace BloggingSystem.Application.Ports;

public interface IAuthorReadRepository
{
    Task<AuthorReadModel?> GetByIdAsync(Guid authorId, CancellationToken ct = default);
    Task UpsertAsync(AuthorReadModel author, CancellationToken ct = default);
}
