using Posts.Application.ReadModels;

namespace Posts.Application.Ports;

public interface IPostReadRepository
{
    Task<PostReadModel?> GetByIdAsync(Guid postId, CancellationToken ct = default);
    Task<(IReadOnlyList<PostReadModel> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken ct = default);
    Task UpsertAsync(PostReadModel post, CancellationToken ct = default);
}
