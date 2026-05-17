using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Authors.Infrastructure.Persistence;

internal sealed class AuthorsDbContextFactory : IDesignTimeDbContextFactory<AuthorsDbContext>
{
    public AuthorsDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AuthorsDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=blogging;Username=postgres;Password=postgres")
            .Options;

        return new AuthorsDbContext(options);
    }
}
