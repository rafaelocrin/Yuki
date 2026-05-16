using BloggingSystem.Application.Ports;
using BloggingSystem.Infrastructure.Persistence.EventStore;
using BloggingSystem.Infrastructure.Persistence.ReadModel;
using BloggingSystem.Infrastructure.Persistence.Seeding;
using BloggingSystem.Infrastructure.Serialization;
using BloggingSystem.Infrastructure.Time;
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
        services.AddSingleton<InMemoryEventStore>();
        services.AddSingleton<IEventStore>(sp => sp.GetRequiredService<InMemoryEventStore>());

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
}
