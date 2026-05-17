using Microsoft.EntityFrameworkCore;
using Posts.Application.ReadModels;
using Posts.Infrastructure.Outbox;

namespace Posts.Infrastructure.Persistence;

internal sealed class PostsDbContext : DbContext
{
    public PostsDbContext(DbContextOptions<PostsDbContext> options) : base(options) { }

    public DbSet<PostReadModel> Posts => Set<PostReadModel>();
    public DbSet<KnownAuthorReadModel> KnownAuthors => Set<KnownAuthorReadModel>();
    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("posts");

        modelBuilder.Entity<PostReadModel>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(p => p.AuthorName).IsRequired();
            entity.Property(p => p.AuthorSurname).IsRequired();
        });

        modelBuilder.Entity<KnownAuthorReadModel>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.ToTable("KnownAuthors");
        });

        modelBuilder.Entity<OutboxEvent>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.Property(o => o.EventType).IsRequired();
            entity.Property(o => o.Payload).IsRequired();
            entity.Property(o => o.ProcessedAt).IsRequired(false);
            entity.HasIndex(o => o.ProcessedAt).HasFilter("\"ProcessedAt\" IS NULL");
            entity.ToTable("OutboxEvents");
        });
    }
}
