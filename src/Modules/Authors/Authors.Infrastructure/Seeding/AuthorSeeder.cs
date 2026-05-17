using Authors.Application.Ports;
using Authors.Application.ReadModels;
using Authors.Contracts;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Authors.Infrastructure.Seeding;

internal sealed class AuthorSeeder : IHostedService
{
    public static readonly Guid Author1Id = new("11111111-1111-1111-1111-111111111111");
    public static readonly Guid Author2Id = new("22222222-2222-2222-2222-222222222222");

    private readonly IServiceScopeFactory _scopeFactory;

    public AuthorSeeder(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await SeedAuthorAsync(Author1Id, "Jane", "Doe", cancellationToken);
        await SeedAuthorAsync(Author2Id, "John", "Smith", cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task SeedAuthorAsync(Guid authorId, string name, string surname, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var authorRepo = scope.ServiceProvider.GetRequiredService<IAuthorReadRepository>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        var existing = await authorRepo.GetByIdAsync(authorId, ct);
        if (existing is not null)
            return;

        var readModel = new AuthorReadModel { Id = authorId, Name = name, Surname = surname };
        await authorRepo.UpsertAsync(readModel, ct);

        // Publish so cross-module handlers (OnAuthorCreated in Posts) can react
        await publisher.Publish(new AuthorCreatedEvent(
            Guid.NewGuid(),
            DateTime.UtcNow,
            new AuthorId(authorId),
            name,
            surname), ct);
    }
}
