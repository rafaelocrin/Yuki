using Posts.Application.Ports;
using Posts.Application.ReadModels;

namespace Posts.Infrastructure.Persistence;

internal sealed class KnownAuthorRepository : IKnownAuthorRepository
{
    private readonly PostsDbContext _context;

    public KnownAuthorRepository(PostsDbContext context)
    {
        _context = context;
    }

    public async Task<KnownAuthorReadModel?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.KnownAuthors.FindAsync(new object[] { id }, ct);
    }

    public async Task UpsertAsync(KnownAuthorReadModel author, CancellationToken ct = default)
    {
        var existing = await _context.KnownAuthors.FindAsync(new object[] { author.Id }, ct);
        if (existing is null)
            _context.KnownAuthors.Add(author);
        else
        {
            existing.Name = author.Name;
            existing.Surname = author.Surname;
        }
        await _context.SaveChangesAsync(ct);
    }
}
