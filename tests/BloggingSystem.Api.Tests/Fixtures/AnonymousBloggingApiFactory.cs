using Authors.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Posts.Infrastructure.Persistence;
using Shared.Application.Ports;
using Shared.Infrastructure.EventStore;

namespace BloggingSystem.Api.Tests.Fixtures;

/// <summary>
/// Factory that does NOT replace the JWT bearer scheme, so unauthenticated requests
/// to protected endpoints correctly return 401. Used by auth-specific tests.
/// </summary>
public sealed class AnonymousBloggingApiFactory : WebApplicationFactory<Program>
{
    private readonly string _postsDbName = Guid.NewGuid().ToString();
    private readonly string _authorsDbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EventStore:Provider"] = "inmemory",
                ["ReadModel:Provider"] = "inmemory"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<PostsDbContext>>();
            services.RemoveAll<PostsDbContext>();
            services.AddDbContext<PostsDbContext>(options =>
                options.UseInMemoryDatabase(_postsDbName));

            services.RemoveAll<DbContextOptions<AuthorsDbContext>>();
            services.RemoveAll<AuthorsDbContext>();
            services.AddDbContext<AuthorsDbContext>(options =>
                options.UseInMemoryDatabase(_authorsDbName));

            services.RemoveAll<IEventStore>();
            services.RemoveAll<InMemoryEventStore>();
            services.AddSingleton<InMemoryEventStore>();
            services.AddSingleton<IEventStore>(sp => sp.GetRequiredService<InMemoryEventStore>());

            services.Configure<HealthCheckServiceOptions>(opts => opts.Registrations.Clear());
            // No auth override — JWT bearer validates tokens normally; missing token → 401.
        });
    }
}
