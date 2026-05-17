using Microsoft.EntityFrameworkCore;
using Posts.Application.Ports;
using Posts.Application.ReadModels;

namespace Posts.Infrastructure.Persistence;

internal sealed class PostReadRepository : IPostReadRepository
{
    private readonly PostsDbContext _context;

    public PostReadRepository(PostsDbContext context)
    {
        _context = context;
    }

    public async Task<PostReadModel?> GetByIdAsync(Guid postId, CancellationToken ct = default)
    {
        return await _context.Posts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == postId, ct);
    }

    public async Task<(IReadOnlyList<PostReadModel> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var baseQuery = _context.Posts.AsNoTracking();
        var totalCount = await baseQuery.CountAsync(ct);
        var items = await baseQuery
            .OrderBy(p => p.CreatedAt)
            .ThenBy(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task UpsertAsync(PostReadModel post, CancellationToken ct = default)
    {
        var existing = await _context.Posts.FindAsync(new object[] { post.Id }, ct);
        if (existing is null)
            _context.Posts.Add(post);
        else
        {
            existing.AuthorId = post.AuthorId;
            existing.AuthorName = post.AuthorName;
            existing.AuthorSurname = post.AuthorSurname;
            existing.Title = post.Title;
            existing.Description = post.Description;
            existing.Content = post.Content;
        }
        await _context.SaveChangesAsync(ct);
    }
}
