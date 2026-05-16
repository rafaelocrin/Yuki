using BloggingSystem.Application.Ports;
using BloggingSystem.Application.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace BloggingSystem.Infrastructure.Persistence.ReadModel;

public sealed class PostReadRepository : IPostReadRepository
{
    private readonly BloggingDbContext _context;

    public PostReadRepository(BloggingDbContext context)
    {
        _context = context;
    }

    public async Task<PostReadModel?> GetByIdAsync(Guid postId, bool includeAuthor, CancellationToken ct = default)
    {
        IQueryable<PostReadModel> query = includeAuthor
            ? _context.Posts.Include(p => p.Author)
            : _context.Posts.AsNoTracking();

        return await query.FirstOrDefaultAsync(p => p.Id == postId, ct);
    }

    public async Task UpsertAsync(PostReadModel post, CancellationToken ct = default)
    {
        var existing = await _context.Posts.FindAsync(new object[] { post.Id }, ct);
        if (existing is null)
            _context.Posts.Add(post);
        else
        {
            existing.AuthorId = post.AuthorId;
            existing.Title = post.Title;
            existing.Description = post.Description;
            existing.Content = post.Content;
        }
        await _context.SaveChangesAsync(ct);
    }
}
