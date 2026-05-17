using Authors.Application.ReadModels;

namespace Authors.Application.Ports;

public interface IAuthorReadRepository
{
    Task<AuthorReadModel?> GetByIdAsync(Guid authorId, CancellationToken ct = default);
    Task UpsertAsync(AuthorReadModel author, CancellationToken ct = default);
}
