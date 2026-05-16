using BloggingSystem.Infrastructure.DependencyInjection;
using BloggingSystem.Infrastructure.Persistence.ReadModel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BloggingSystem.Api.Tests.Fixtures;

public sealed class BloggingApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace the InfrastructureServiceExtensions DbContext with a unique InMemory instance per factory
            services.RemoveAll<DbContextOptions<BloggingDbContext>>();
            services.RemoveAll<BloggingDbContext>();

            services.AddDbContext<BloggingDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));
        });
    }
}
