using BloggingSystem.Application.Ports;
using BloggingSystem.Application.ReadModels;

namespace BloggingSystem.Infrastructure.Persistence.ReadModel;

public sealed class AuthorReadRepository : IAuthorReadRepository
{
    private readonly BloggingDbContext _context;

    public AuthorReadRepository(BloggingDbContext context)
    {
        _context = context;
    }

    public async Task<AuthorReadModel?> GetByIdAsync(Guid authorId, CancellationToken ct = default)
    {
        return await _context.Authors.FindAsync(new object[] { authorId }, ct);
    }

    public async Task UpsertAsync(AuthorReadModel author, CancellationToken ct = default)
    {
        var existing = await _context.Authors.FindAsync(new object[] { author.Id }, ct);
        if (existing is null)
            _context.Authors.Add(author);
        else
        {
            existing.Name = author.Name;
            existing.Surname = author.Surname;
        }
        await _context.SaveChangesAsync(ct);
    }
}
