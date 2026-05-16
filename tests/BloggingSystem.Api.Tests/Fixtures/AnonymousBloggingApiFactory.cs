using BloggingSystem.Application.Ports;
using BloggingSystem.Infrastructure.Persistence.EventStore;
using BloggingSystem.Infrastructure.Persistence.ReadModel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BloggingSystem.Api.Tests.Fixtures;

/// <summary>
/// Factory that does NOT replace the JWT bearer scheme, so unauthenticated requests
/// to protected endpoints correctly return 401. Used by auth-specific tests.
/// </summary>
public sealed class AnonymousBloggingApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

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
            services.RemoveAll<DbContextOptions<BloggingDbContext>>();
            services.RemoveAll<BloggingDbContext>();
            services.AddDbContext<BloggingDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));

            services.RemoveAll<IEventStore>();
            services.RemoveAll<InMemoryEventStore>();
            services.AddSingleton<InMemoryEventStore>();
            services.AddSingleton<IEventStore>(sp => sp.GetRequiredService<InMemoryEventStore>());

            services.Configure<HealthCheckServiceOptions>(opts => opts.Registrations.Clear());
            // No auth override — JWT bearer validates tokens normally; missing token → 401.
        });
    }
}
