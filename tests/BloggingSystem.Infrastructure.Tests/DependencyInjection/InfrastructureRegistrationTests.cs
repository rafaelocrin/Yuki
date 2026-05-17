using Authors.Infrastructure.DependencyInjection;
using Authors.Infrastructure.Persistence;
using Posts.Application.Ports;
using Posts.Infrastructure.DependencyInjection;
using Posts.Infrastructure.Outbox;
using Posts.Infrastructure.Persistence;
using Shared.Application.Ports;
using Shared.Infrastructure.DependencyInjection;
using Shared.Infrastructure.EventStore;
using Shared.Infrastructure.Serialization;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BloggingSystem.Infrastructure.Tests.DependencyInjection;

[Trait("Category", "Integration")]
public sealed class InfrastructureRegistrationTests
{
    private static IConfiguration BuildConfig(params (string key, string value)[] entries) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(entries.Select(e => new KeyValuePair<string, string?>(e.key, e.value)))
            .Build();

    // ── Serializer switch ──────────────────────────────────────────────────

    [Fact]
    public void AddSharedInfrastructure_WithJsonFormat_RegistersJsonSerializer()
    {
        var config = BuildConfig(("Serialization:Format", "json"));
        var services = new ServiceCollection().AddLogging();
        services.AddSharedInfrastructure(config);

        services.Should().Contain(d =>
            d.ServiceType == typeof(IMessageSerializer) &&
            d.ImplementationType == typeof(JsonMessageSerializer));
    }

    [Fact]
    public void AddSharedInfrastructure_WithXmlFormat_RegistersXmlSerializer()
    {
        var config = BuildConfig(("Serialization:Format", "xml"));
        var services = new ServiceCollection().AddLogging();
        services.AddSharedInfrastructure(config);

        services.Should().Contain(d =>
            d.ServiceType == typeof(IMessageSerializer) &&
            d.ImplementationType == typeof(XmlMessageSerializer));
    }

    [Fact]
    public void AddSharedInfrastructure_WithNoSerializationKey_DefaultsToJson()
    {
        var config = BuildConfig();
        var services = new ServiceCollection().AddLogging();
        services.AddSharedInfrastructure(config);

        services.Should().Contain(d =>
            d.ServiceType == typeof(IMessageSerializer) &&
            d.ImplementationType == typeof(JsonMessageSerializer));
    }

    // ── Event store switch ─────────────────────────────────────────────────

    [Fact]
    public void AddSharedInfrastructure_WithInMemoryProvider_RegistersInMemoryEventStore()
    {
        var config = BuildConfig(("EventStore:Provider", "inmemory"));
        var services = new ServiceCollection().AddLogging();
        services.AddSharedInfrastructure(config);

        services.Should().Contain(d => d.ServiceType == typeof(InMemoryEventStore));
        services.Should().Contain(d => d.ServiceType == typeof(IEventStore));
    }

    [Fact]
    public void AddSharedInfrastructure_WithNoEventStoreKey_DefaultsToInMemory()
    {
        var config = BuildConfig();
        var services = new ServiceCollection().AddLogging();
        services.AddSharedInfrastructure(config);

        services.Should().Contain(d => d.ServiceType == typeof(InMemoryEventStore));
    }

    [Fact]
    public void AddSharedInfrastructure_WithMartenProvider_RegistersMartenEventStore()
    {
        var config = BuildConfig(
            ("EventStore:Provider", "marten"),
            ("ConnectionStrings:PostgreSQL", "Host=localhost;Database=blogging;Username=postgres;Password=postgres"));

        var services = new ServiceCollection().AddLogging();
        services.AddSharedInfrastructure(config);

        services.Should().Contain(d =>
            d.ServiceType == typeof(IEventStore) &&
            d.ImplementationType == typeof(MartenEventStore));
    }

    [Fact]
    public void AddSharedInfrastructure_WithMartenProvider_DoesNotRegisterInMemoryEventStore()
    {
        var config = BuildConfig(
            ("EventStore:Provider", "marten"),
            ("ConnectionStrings:PostgreSQL", "Host=localhost;Database=blogging;Username=postgres;Password=postgres"));

        var services = new ServiceCollection().AddLogging();
        services.AddSharedInfrastructure(config);

        services.Should().NotContain(d => d.ServiceType == typeof(InMemoryEventStore));
    }

    [Fact]
    public void AddSharedInfrastructure_WithMartenProvider_MissingConnectionString_Throws()
    {
        var config = BuildConfig(("EventStore:Provider", "marten"));
        var services = new ServiceCollection().AddLogging();
        var act = () => services.AddSharedInfrastructure(config);

        act.Should().Throw<InvalidOperationException>().WithMessage("*PostgreSQL*");
    }

    // ── Read model switch ──────────────────────────────────────────────────

    [Fact]
    public void AddPostsModule_WithNoReadModelKey_DefaultsToInMemory()
    {
        var config = BuildConfig();
        var services = new ServiceCollection().AddLogging();
        services.AddPostsModule(config, $"test-{Guid.NewGuid()}");

        services.Should().NotContain(d =>
            d.ServiceType == typeof(IHostedService) &&
            d.ImplementationType == typeof(PostsDatabaseMigrator));
    }

    [Fact]
    public void AddPostsModule_WithInMemoryReadModel_DoesNotRegisterDatabaseMigrator()
    {
        var config = BuildConfig(("ReadModel:Provider", "inmemory"));
        var services = new ServiceCollection().AddLogging();
        services.AddPostsModule(config, $"test-{Guid.NewGuid()}");

        services.Should().NotContain(d =>
            d.ServiceType == typeof(IHostedService) &&
            d.ImplementationType == typeof(PostsDatabaseMigrator));
    }

    [Fact]
    public void AddPostsModule_WithPostgresqlReadModel_RegistersDatabaseMigrator()
    {
        var config = BuildConfig(
            ("ReadModel:Provider", "postgresql"),
            ("ConnectionStrings:PostgreSQL", "Host=localhost;Database=blogging;Username=postgres;Password=postgres"));

        var services = new ServiceCollection().AddLogging();
        services.AddPostsModule(config, $"test-{Guid.NewGuid()}");

        services.Should().Contain(d =>
            d.ServiceType == typeof(IHostedService) &&
            d.ImplementationType == typeof(PostsDatabaseMigrator));
    }

    [Fact]
    public void AddPostsModule_WithPostgresqlReadModel_MissingConnectionString_Throws()
    {
        var config = BuildConfig(("ReadModel:Provider", "postgresql"));
        var services = new ServiceCollection().AddLogging();
        var act = () => services.AddPostsModule(config, $"test-{Guid.NewGuid()}");

        act.Should().Throw<InvalidOperationException>().WithMessage("*PostgreSQL*");
    }

    // ── Outbox registration ────────────────────────────────────────────────

    [Fact]
    public void AddPostsModule_RegistersOutboxWriter()
    {
        var config = BuildConfig();
        var services = new ServiceCollection().AddLogging();
        services.AddPostsModule(config, $"test-{Guid.NewGuid()}");

        services.Should().Contain(d =>
            d.ServiceType == typeof(IOutboxWriter) &&
            d.ImplementationType == typeof(EfCoreOutboxWriter));
    }

    [Fact]
    public void AddPostsModule_RegistersOutboxProcessor()
    {
        var config = BuildConfig();
        var services = new ServiceCollection().AddLogging();
        services.AddPostsModule(config, $"test-{Guid.NewGuid()}");

        services.Should().Contain(d =>
            d.ServiceType == typeof(IHostedService) &&
            d.ImplementationType == typeof(OutboxProcessor));
    }
}
