using Authors.Application.Ports;
using Authors.Application.ReadModels;

namespace Authors.Infrastructure.Persistence;

internal sealed class AuthorReadRepository : IAuthorReadRepository
{
    private readonly AuthorsDbContext _context;

    public AuthorReadRepository(AuthorsDbContext context)
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
