using Authors.Infrastructure.Persistence;
using Authors.Infrastructure.Seeding;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Posts.Application.Ports;
using Posts.Infrastructure.Persistence;
using Shared.Application.Ports;
using Shared.Infrastructure.EventStore;

namespace BloggingSystem.Api.Tests.Fixtures;

/// <summary>
/// Factory that does NOT replace the JWT bearer scheme, so unauthenticated requests
/// to protected endpoints correctly return 401. Used by auth-specific tests.
/// </summary>
public sealed class AnonymousBloggingApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _postsDbName   = Guid.NewGuid().ToString();
    private readonly string _authorsDbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EventStore:Provider"]  = "inmemory",
                ["ReadModel:Provider"]   = "inmemory",
                ["MessageBus:Transport"] = "inmemory"
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

    public async Task InitializeAsync()
    {
        _ = Server;

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await WaitForKnownAuthorsAsync(cts.Token);
    }

    public new Task DisposeAsync() => base.DisposeAsync().AsTask();

    private async Task WaitForKnownAuthorsAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            using var scope = Services.CreateScope();
            var repo   = scope.ServiceProvider.GetRequiredService<IKnownAuthorRepository>();
            var author = await repo.GetByIdAsync(AuthorSeeder.Author1Id, ct);
            if (author is not null) return;
            await Task.Delay(20, ct);
        }
        throw new TimeoutException("KnownAuthors were not populated within 10 seconds.");
    }
}
