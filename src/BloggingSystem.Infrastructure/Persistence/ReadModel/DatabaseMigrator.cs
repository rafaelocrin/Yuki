using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BloggingSystem.Infrastructure.Persistence.ReadModel;

public sealed class DatabaseMigrator : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public DatabaseMigrator(IServiceScopeFactory scopeFactory)
        => _scopeFactory = scopeFactory;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BloggingDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
