using Posts.Application.ReadModels;

namespace Posts.Application.Ports;

public interface IKnownAuthorRepository
{
    Task<KnownAuthorReadModel?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task UpsertAsync(KnownAuthorReadModel author, CancellationToken ct = default);
}
