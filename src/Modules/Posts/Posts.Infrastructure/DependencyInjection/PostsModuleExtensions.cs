using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Posts.Application.Ports;
using Posts.Application.Projections;
using Posts.Infrastructure.Outbox;
using Posts.Infrastructure.Persistence;
using Shared.Application.Ports;

namespace Posts.Infrastructure.DependencyInjection;

public static class PostsModuleExtensions
{
    public static IServiceCollection AddPostsModule(
        this IServiceCollection services,
        IConfiguration configuration,
        string? dbName = null)
    {
        var provider = configuration["ReadModel:Provider"] ?? "inmemory";

        if (provider.Equals("postgresql", StringComparison.OrdinalIgnoreCase))
        {
            var connectionString = configuration.GetConnectionString("PostgreSQL")
                ?? throw new InvalidOperationException(
                    "Connection string 'PostgreSQL' is required when ReadModel:Provider is 'postgresql'.");

            services.AddDbContext<PostsDbContext>(options =>
                options.UseNpgsql(connectionString));

            services.AddHostedService<PostsDatabaseMigrator>();
        }
        else
        {
            services.AddDbContext<PostsDbContext>(options =>
                options.UseInMemoryDatabase(dbName ?? "PostsDb"));
        }

        services.AddScoped<IPostReadRepository, PostReadRepository>();
        services.AddScoped<IKnownAuthorRepository, KnownAuthorRepository>();
        services.AddScoped<IOutboxWriter, EfCoreOutboxWriter>();
        services.AddHostedService<OutboxProcessor>();
        services.AddScoped<PostProjection>();

        return services;
    }
}
