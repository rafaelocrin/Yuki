using Authors.Infrastructure.Persistence;
using Authors.Infrastructure.Seeding;
using Microsoft.AspNetCore.Authentication;
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

public sealed class BloggingApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
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
            // Replace PostsDbContext with unique InMemory instance per factory
            services.RemoveAll<DbContextOptions<PostsDbContext>>();
            services.RemoveAll<PostsDbContext>();
            services.AddDbContext<PostsDbContext>(options =>
                options.UseInMemoryDatabase(_postsDbName));

            // Replace AuthorsDbContext with unique InMemory instance per factory
            services.RemoveAll<DbContextOptions<AuthorsDbContext>>();
            services.RemoveAll<AuthorsDbContext>();
            services.AddDbContext<AuthorsDbContext>(options =>
                options.UseInMemoryDatabase(_authorsDbName));

            // Replace event store with InMemory
            services.RemoveAll<IEventStore>();
            services.RemoveAll<InMemoryEventStore>();
            services.AddSingleton<InMemoryEventStore>();
            services.AddSingleton<IEventStore>(sp => sp.GetRequiredService<InMemoryEventStore>());

            // Clear any PostgreSQL health check probes
            services.Configure<HealthCheckServiceOptions>(opts => opts.Registrations.Clear());

            // Replace JWT bearer with a test scheme
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme    = TestAuthHandler.SchemeName;
                options.DefaultScheme             = TestAuthHandler.SchemeName;
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }

    public async Task InitializeAsync()
    {
        // Force host startup (triggers AuthorSeeder which publishes AuthorCreatedEvents to the bus)
        _ = Server;

        // Wait for AuthorCreatedConsumer to populate KnownAuthors before any test runs.
        // In-memory MassTransit delivers asynchronously, so we poll until seeded authors appear.
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
