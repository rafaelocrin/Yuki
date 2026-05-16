using BloggingSystem.Application.ReadModels;
using BloggingSystem.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;

namespace BloggingSystem.Infrastructure.Persistence.ReadModel;

public sealed class BloggingDbContext : DbContext
{
    public BloggingDbContext(DbContextOptions<BloggingDbContext> options) : base(options) { }

    public DbSet<PostReadModel> Posts => Set<PostReadModel>();
    public DbSet<AuthorReadModel> Authors => Set<AuthorReadModel>();
    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PostReadModel>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.CreatedAt)
                  .HasDefaultValueSql("NOW()");
            entity.HasOne(p => p.Author)
                  .WithMany()
                  .HasForeignKey(p => p.AuthorId)
                  .IsRequired(false);
        });

        modelBuilder.Entity<AuthorReadModel>(entity =>
        {
            entity.HasKey(a => a.Id);
        });

        modelBuilder.Entity<OutboxEvent>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.Property(o => o.EventType).IsRequired();
            entity.Property(o => o.Payload).IsRequired();
            entity.Property(o => o.ProcessedAt).IsRequired(false);
            // Partial index on unprocessed entries — efficient for the processor poll.
            entity.HasIndex(o => o.ProcessedAt).HasFilter("\"ProcessedAt\" IS NULL");
            entity.ToTable("OutboxEvents");
        });
    }
}
