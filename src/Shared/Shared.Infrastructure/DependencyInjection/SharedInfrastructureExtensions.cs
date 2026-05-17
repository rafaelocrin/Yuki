using MassTransit;
using Shared.Application.Ports;
using Shared.Domain.Events;
using Shared.Infrastructure.EventStore;
using Shared.Infrastructure.Messaging;
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
        IConfiguration configuration,
        Action<IBusRegistrationConfigurator>? registerConsumers = null)
    {
        RegisterEventStore(services, configuration);
        RegisterSerializer(services, configuration);
        RegisterHealthChecks(services, configuration);
        RegisterMessageBus(services, configuration, registerConsumers);

        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        return services;
    }

    private static void RegisterMessageBus(
        IServiceCollection services,
        IConfiguration configuration,
        Action<IBusRegistrationConfigurator>? registerConsumers)
    {
        var transport = configuration["MessageBus:Transport"] ?? "inmemory";

        services.AddMassTransit(x =>
        {
            registerConsumers?.Invoke(x);

            if (transport.Equals("rabbitmq", StringComparison.OrdinalIgnoreCase))
            {
                var host     = configuration["MessageBus:RabbitMQ:Host"]     ?? "localhost";
                var username = configuration["MessageBus:RabbitMQ:Username"] ?? "guest";
                var password = configuration["MessageBus:RabbitMQ:Password"] ?? "guest";

                x.UsingRabbitMq((ctx, cfg) =>
                {
                    cfg.Host(host, h =>
                    {
                        h.Username(username);
                        h.Password(password);
                    });
                    cfg.ConfigureEndpoints(ctx);
                });
            }
            else
            {
                x.UsingInMemory((ctx, cfg) => cfg.ConfigureEndpoints(ctx));
            }
        });

        services.AddScoped<IIntegrationEventPublisher, MassTransitIntegrationEventPublisher>();
    }

    private static void RegisterSerializer(IServiceCollection services, IConfiguration configuration)
    {
        var format = configuration["Serialization:Format"] ?? "json";
        if (format.Equals("xml", StringComparison.OrdinalIgnoreCase))
            services.AddSingleton<Shared.Application.Ports.IMessageSerializer, XmlMessageSerializer>();
        else
            services.AddSingleton<Shared.Application.Ports.IMessageSerializer, JsonMessageSerializer>();
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
