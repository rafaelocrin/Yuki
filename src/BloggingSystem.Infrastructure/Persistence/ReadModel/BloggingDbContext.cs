using BloggingSystem.Application.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace BloggingSystem.Infrastructure.Persistence.ReadModel;

public sealed class BloggingDbContext : DbContext
{
    public BloggingDbContext(DbContextOptions<BloggingDbContext> options) : base(options) { }

    public DbSet<PostReadModel> Posts => Set<PostReadModel>();
    public DbSet<AuthorReadModel> Authors => Set<AuthorReadModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PostReadModel>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.HasOne(p => p.Author)
                  .WithMany()
                  .HasForeignKey(p => p.AuthorId)
                  .IsRequired(false);
        });

        modelBuilder.Entity<AuthorReadModel>(entity =>
        {
            entity.HasKey(a => a.Id);
        });
    }
}
