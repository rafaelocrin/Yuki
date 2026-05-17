using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Posts.Infrastructure.Persistence;

internal sealed class PostsDbContextFactory : IDesignTimeDbContextFactory<PostsDbContext>
{
    public PostsDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PostsDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=blogging;Username=postgres;Password=postgres")
            .Options;

        return new PostsDbContext(options);
    }
}
