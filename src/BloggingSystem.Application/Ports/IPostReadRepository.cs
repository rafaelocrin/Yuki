using BloggingSystem.Application.ReadModels;

namespace BloggingSystem.Application.Ports;

public interface IPostReadRepository
{
    Task<PostReadModel?> GetByIdAsync(Guid postId, bool includeAuthor, CancellationToken ct = default);
    Task<(IReadOnlyList<PostReadModel> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, bool includeAuthor, CancellationToken ct = default);
    Task UpsertAsync(PostReadModel post, CancellationToken ct = default);
}
