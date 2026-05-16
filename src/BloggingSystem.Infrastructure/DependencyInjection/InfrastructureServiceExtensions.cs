using BloggingSystem.Application.Ports;
using BloggingSystem.Domain.Events;
using BloggingSystem.Infrastructure.Persistence.EventStore;
using BloggingSystem.Infrastructure.Persistence.ReadModel;
using BloggingSystem.Infrastructure.Persistence.Seeding;
using BloggingSystem.Infrastructure.Serialization;
using BloggingSystem.Infrastructure.Time;
using Marten;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BloggingSystem.Infrastructure.DependencyInjection;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        string? dbName = null)
    {
        RegisterEventStore(services, configuration);

        services.AddDbContext<BloggingDbContext>(options =>
            options.UseInMemoryDatabase(dbName ?? "BloggingDb"));

        services.AddScoped<IPostReadRepository, PostReadRepository>();
        services.AddScoped<IAuthorReadRepository, AuthorReadRepository>();

        var format = configuration["Serialization:Format"] ?? "json";
        if (format.Equals("xml", StringComparison.OrdinalIgnoreCase))
            services.AddSingleton<IMessageSerializer, XmlMessageSerializer>();
        else
            services.AddSingleton<IMessageSerializer, JsonMessageSerializer>();

        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        services.AddHostedService<DataSeeder>();

        return services;
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
