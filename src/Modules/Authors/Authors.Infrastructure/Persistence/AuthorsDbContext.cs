using Authors.Application.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace Authors.Infrastructure.Persistence;

internal sealed class AuthorsDbContext : DbContext
{
    public AuthorsDbContext(DbContextOptions<AuthorsDbContext> options) : base(options) { }

    public DbSet<AuthorReadModel> Authors => Set<AuthorReadModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("authors");

        modelBuilder.Entity<AuthorReadModel>(entity =>
        {
            entity.HasKey(a => a.Id);
        });
    }
}
