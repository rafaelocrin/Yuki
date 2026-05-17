using Authors.Application.Ports;
using Authors.Application.Projections;
using Authors.Infrastructure.Idempotency;
using Authors.Infrastructure.Persistence;
using Authors.Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Ports;

namespace Authors.Infrastructure.DependencyInjection;

public static class AuthorsModuleExtensions
{
    public static IServiceCollection AddAuthorsModule(
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

            services.AddDbContext<AuthorsDbContext>(options =>
                options.UseNpgsql(connectionString, npgsql =>
                    npgsql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorCodesToAdd: null)));

            services.AddHostedService<AuthorsDatabaseMigrator>();
        }
        else
        {
            services.AddDbContext<AuthorsDbContext>(options =>
                options.UseInMemoryDatabase(dbName ?? "AuthorsDb"));
        }

        services.AddScoped<IAuthorReadRepository, AuthorReadRepository>();
        services.AddScoped<IProcessedCommandRepository, EfCoreProcessedCommandRepository>();
        services.AddScoped<AuthorProjection>();
        services.AddHostedService<AuthorSeeder>();

        return services;
    }
}
