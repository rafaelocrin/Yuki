using BloggingSystem.Application.Ports;
using BloggingSystem.Domain.Events;
using BloggingSystem.Infrastructure.Outbox;
using BloggingSystem.Infrastructure.Persistence.EventStore;
using BloggingSystem.Infrastructure.Persistence.ReadModel;
using BloggingSystem.Infrastructure.Persistence.Seeding;
using BloggingSystem.Infrastructure.Serialization;
using BloggingSystem.Infrastructure.Time;
using Marten;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BloggingSystem.Infrastructure.DependencyInjection;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        string? dbName = null)
    {
        RegisterEventStore(services, configuration);
        RegisterReadModel(services, configuration, dbName);
        RegisterHealthChecks(services, configuration);

        services.AddScoped<IPostReadRepository, PostReadRepository>();
        services.AddScoped<IAuthorReadRepository, AuthorReadRepository>();

        var format = configuration["Serialization:Format"] ?? "json";
        if (format.Equals("xml", StringComparison.OrdinalIgnoreCase))
            services.AddSingleton<IMessageSerializer, XmlMessageSerializer>();
        else
            services.AddSingleton<IMessageSerializer, JsonMessageSerializer>();

        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        services.AddScoped<IOutboxWriter, EfCoreOutboxWriter>();
        services.AddHostedService<OutboxProcessor>();

        services.AddHostedService<DataSeeder>();

        return services;
    }

    private static void RegisterReadModel(IServiceCollection services, IConfiguration configuration, string? dbName)
    {
        var provider = configuration["ReadModel:Provider"] ?? "inmemory";

        if (provider.Equals("postgresql", StringComparison.OrdinalIgnoreCase))
        {
            var connectionString = configuration.GetConnectionString("PostgreSQL")
                ?? throw new InvalidOperationException(
                    "Connection string 'PostgreSQL' is required when ReadModel:Provider is 'postgresql'.");

            services.AddDbContext<BloggingDbContext>(options =>
                options.UseNpgsql(connectionString));

            // Runs migrations before DataSeeder (hosted services execute in registration order).
            services.AddHostedService<DatabaseMigrator>();
        }
        else
        {
            services.AddDbContext<BloggingDbContext>(options =>
                options.UseInMemoryDatabase(dbName ?? "BloggingDb"));
        }
    }

    private static void RegisterHealthChecks(IServiceCollection services, IConfiguration configuration)
    {
        var builder = services.AddHealthChecks();

        var readModelProvider = configuration["ReadModel:Provider"] ?? "inmemory";
        var eventStoreProvider = configuration["EventStore:Provider"] ?? "inmemory";
        var needsPostgres = readModelProvider.Equals("postgresql", StringComparison.OrdinalIgnoreCase)
            || eventStoreProvider.Equals("marten", StringComparison.OrdinalIgnoreCase);

        if (needsPostgres)
        {
            var connectionString = configuration.GetConnectionString("PostgreSQL");
            if (connectionString is not null)
                builder.AddNpgSql(
                    connectionString,
                    name: "postgresql",
                    failureStatus: HealthStatus.Unhealthy,
                    tags: new[] { "ready" });
        }
    }

    private static void RegisterEventStore(IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["EventStore:Provider"] ?? "inmemory";

        if (provider.Equals("marten", StringComparison.OrdinalIgnoreCase))
        {
            var connectionString = configuration.GetConnectionString("PostgreSQL")
                ?? throw new InvalidOperationException(
                    "Connection string 'PostgreSQL' is required when EventStore:Provider is 'marten'.");

            services.AddMarten(opts =>
            {
                opts.Connection(connectionString);

                // Register all IDomainEvent implementations so Marten can deserialize them.
                var eventTypes = typeof(IDomainEvent).Assembly
                    .GetTypes()
                    .Where(t => !t.IsAbstract && !t.IsInterface && typeof(IDomainEvent).IsAssignableFrom(t));

                foreach (var type in eventTypes)
                    opts.Events.AddEventType(type);
            });

            services.AddSingleton<IEventStore, MartenEventStore>();
        }
        else
        {
            services.AddSingleton<InMemoryEventStore>();
            services.AddSingleton<IEventStore>(sp => sp.GetRequiredService<InMemoryEventStore>());
        }
    }
}
