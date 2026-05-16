using BloggingSystem.Application.Ports;
using BloggingSystem.Infrastructure.DependencyInjection;
using BloggingSystem.Infrastructure.Persistence.EventStore;
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
            // Replace DbContext with a unique InMemory instance per factory for test isolation.
            services.RemoveAll<DbContextOptions<BloggingDbContext>>();
            services.RemoveAll<BloggingDbContext>();
            services.AddDbContext<BloggingDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));

            // Replace event store with InMemory so tests never touch PostgreSQL/Marten,
            // regardless of what appsettings.Development.json configures.
            services.RemoveAll<IEventStore>();
            services.RemoveAll<InMemoryEventStore>();
            services.AddSingleton<InMemoryEventStore>();
            services.AddSingleton<IEventStore>(sp => sp.GetRequiredService<InMemoryEventStore>());
        });
    }
}
