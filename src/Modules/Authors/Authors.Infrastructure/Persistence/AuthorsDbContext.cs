using Authors.Application.ReadModels;
using Authors.Infrastructure.Idempotency;
using Microsoft.EntityFrameworkCore;

namespace Authors.Infrastructure.Persistence;

internal sealed class AuthorsDbContext : DbContext
{
    public AuthorsDbContext(DbContextOptions<AuthorsDbContext> options) : base(options) { }

    public DbSet<AuthorReadModel> Authors => Set<AuthorReadModel>();
    public DbSet<ProcessedCommand> ProcessedCommands => Set<ProcessedCommand>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("authors");

        modelBuilder.Entity<AuthorReadModel>(entity =>
        {
            entity.HasKey(a => a.Id);
        });

        modelBuilder.Entity<ProcessedCommand>(entity =>
        {
            entity.HasKey(p => p.IdempotencyKey);
            entity.Property(p => p.SerializedResult).IsRequired();
            entity.ToTable("ProcessedCommands");
        });
    }
}
