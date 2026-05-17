using Authors.Application.Ports;
using Authors.Application.Projections;
using Authors.Infrastructure.Persistence;
using Authors.Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
                options.UseNpgsql(connectionString));

            services.AddHostedService<AuthorsDatabaseMigrator>();
        }
        else
        {
            services.AddDbContext<AuthorsDbContext>(options =>
                options.UseInMemoryDatabase(dbName ?? "AuthorsDb"));
        }

        services.AddScoped<IAuthorReadRepository, AuthorReadRepository>();
        services.AddScoped<AuthorProjection>();
        services.AddHostedService<AuthorSeeder>();

        return services;
    }
}
