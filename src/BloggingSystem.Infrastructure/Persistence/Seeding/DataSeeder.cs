using BloggingSystem.Application.Ports;
using BloggingSystem.Application.ReadModels;
using BloggingSystem.Domain.Aggregates.Author;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BloggingSystem.Infrastructure.Persistence.Seeding;

public sealed class DataSeeder : IHostedService
{
    public static readonly Guid Author1Id = new("11111111-1111-1111-1111-111111111111");
    public static readonly Guid Author2Id = new("22222222-2222-2222-2222-222222222222");

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IEventStore _eventStore;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DataSeeder(
        IServiceScopeFactory scopeFactory,
        IEventStore eventStore,
        IDateTimeProvider dateTimeProvider)
    {
        _scopeFactory = scopeFactory;
        _eventStore = eventStore;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var authorRepo = scope.ServiceProvider.GetRequiredService<IAuthorReadRepository>();

        await SeedAuthorAsync(authorRepo, Author1Id, "Jane", "Doe", cancellationToken);
        await SeedAuthorAsync(authorRepo, Author2Id, "John", "Smith", cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task SeedAuthorAsync(
        IAuthorReadRepository repo,
        Guid authorId,
        string name,
        string surname,
        CancellationToken ct)
    {
        var existing = await repo.GetByIdAsync(authorId, ct);
        if (existing is not null)
            return;

        var author = Author.Create(name, surname, _dateTimeProvider.UtcNow);

        var readModel = new AuthorReadModel { Id = authorId, Name = name, Surname = surname };
        await repo.UpsertAsync(readModel, ct);

        await _eventStore.AppendEventsAsync(authorId, author.UncommittedEvents, ct);
        author.ClearUncommittedEvents();
    }
}
