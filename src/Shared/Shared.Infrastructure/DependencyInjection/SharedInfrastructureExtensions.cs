using Shared.Application.Ports;
using Shared.Domain.Events;
using Shared.Infrastructure.EventStore;
using Shared.Infrastructure.Serialization;
using Shared.Infrastructure.Time;
using Marten;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Shared.Infrastructure.DependencyInjection;

public static class SharedInfrastructureExtensions
{
    public static IServiceCollection AddSharedInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        RegisterEventStore(services, configuration);
        RegisterSerializer(services, configuration);
        RegisterHealthChecks(services, configuration);

        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        return services;
    }

    private static void RegisterSerializer(IServiceCollection services, IConfiguration configuration)
    {
        var format = configuration["Serialization:Format"] ?? "json";
        if (format.Equals("xml", StringComparison.OrdinalIgnoreCase))
            services.AddSingleton<IMessageSerializer, XmlMessageSerializer>();
        else
            services.AddSingleton<IMessageSerializer, JsonMessageSerializer>();
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

                // Register all IDomainEvent implementations from all loaded assemblies
                var eventTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => { try { return a.GetTypes(); } catch { return Array.Empty<Type>(); } })
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
}
