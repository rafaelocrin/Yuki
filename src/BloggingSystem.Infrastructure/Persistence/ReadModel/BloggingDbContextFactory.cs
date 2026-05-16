using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BloggingSystem.Infrastructure.Persistence.ReadModel;

public sealed class BloggingDbContextFactory : IDesignTimeDbContextFactory<BloggingDbContext>
{
    public BloggingDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<BloggingDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=blogging;Username=postgres;Password=postgres")
            .Options;

        return new BloggingDbContext(options);
    }
}
