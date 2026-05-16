using BloggingSystem.Application.Ports;
using BloggingSystem.Infrastructure.DependencyInjection;
using BloggingSystem.Infrastructure.Persistence.EventStore;
using BloggingSystem.Infrastructure.Serialization;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BloggingSystem.Infrastructure.Tests.DependencyInjection;

public sealed class InfrastructureRegistrationTests
{
    private static IConfiguration BuildConfig(params (string key, string value)[] entries) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(entries.Select(e => new KeyValuePair<string, string?>(e.key, e.value)))
            .Build();

    // ── Serializer switch ──────────────────────────────────────────────────

    [Fact]
    public void AddInfrastructure_WithJsonFormat_RegistersJsonSerializer()
    {
        var config = BuildConfig(("Serialization:Format", "json"));
        var services = new ServiceCollection().AddLogging();
        services.AddInfrastructure(config, $"test-{Guid.NewGuid()}");

        services.Should().Contain(d =>
            d.ServiceType == typeof(IMessageSerializer) &&
            d.ImplementationType == typeof(JsonMessageSerializer));
    }

    [Fact]
    public void AddInfrastructure_WithXmlFormat_RegistersXmlSerializer()
    {
        var config = BuildConfig(("Serialization:Format", "xml"));
        var services = new ServiceCollection().AddLogging();
        services.AddInfrastructure(config, $"test-{Guid.NewGuid()}");

        services.Should().Contain(d =>
            d.ServiceType == typeof(IMessageSerializer) &&
            d.ImplementationType == typeof(XmlMessageSerializer));
    }

    [Fact]
    public void AddInfrastructure_WithNoSerializationKey_DefaultsToJson()
    {
        var config = BuildConfig();
        var services = new ServiceCollection().AddLogging();
        services.AddInfrastructure(config, $"test-{Guid.NewGuid()}");

        services.Should().Contain(d =>
            d.ServiceType == typeof(IMessageSerializer) &&
            d.ImplementationType == typeof(JsonMessageSerializer));
    }

    // ── Event store switch ─────────────────────────────────────────────────

    [Fact]
    public void AddInfrastructure_WithInMemoryProvider_RegistersInMemoryEventStore()
    {
        var config = BuildConfig(("EventStore:Provider", "inmemory"));
        var services = new ServiceCollection().AddLogging();
        services.AddInfrastructure(config, $"test-{Guid.NewGuid()}");

        // The IEventStore descriptor for InMemory uses a factory delegate (not ImplementationType).
        services.Should().Contain(d =>
            d.ServiceType == typeof(InMemoryEventStore));
        services.Should().Contain(d =>
            d.ServiceType == typeof(IEventStore));
    }

    [Fact]
    public void AddInfrastructure_WithNoEventStoreKey_DefaultsToInMemory()
    {
        var config = BuildConfig();
        var services = new ServiceCollection().AddLogging();
        services.AddInfrastructure(config, $"test-{Guid.NewGuid()}");

        services.Should().Contain(d => d.ServiceType == typeof(InMemoryEventStore));
    }

    [Fact]
    public void AddInfrastructure_WithMartenProvider_RegistersMartenEventStore()
    {
        var config = BuildConfig(
            ("EventStore:Provider", "marten"),
            ("ConnectionStrings:PostgreSQL", "Host=localhost;Database=blogging;Username=postgres;Password=postgres"));

        var services = new ServiceCollection().AddLogging();
        services.AddInfrastructure(config, $"test-{Guid.NewGuid()}");

        // Verify MartenEventStore is registered as IEventStore (no PostgreSQL needed — just inspecting descriptors).
        services.Should().Contain(d =>
            d.ServiceType == typeof(IEventStore) &&
            d.ImplementationType == typeof(MartenEventStore));
    }

    [Fact]
    public void AddInfrastructure_WithMartenProvider_DoesNotRegisterInMemoryEventStore()
    {
        var config = BuildConfig(
            ("EventStore:Provider", "marten"),
            ("ConnectionStrings:PostgreSQL", "Host=localhost;Database=blogging;Username=postgres;Password=postgres"));

        var services = new ServiceCollection().AddLogging();
        services.AddInfrastructure(config, $"test-{Guid.NewGuid()}");

        services.Should().NotContain(d => d.ServiceType == typeof(InMemoryEventStore));
    }

    [Fact]
    public void AddInfrastructure_WithMartenProvider_MissingConnectionString_Throws()
    {
        var config = BuildConfig(("EventStore:Provider", "marten")); // no connection string

        var services = new ServiceCollection().AddLogging();
        var act = () => services.AddInfrastructure(config, $"test-{Guid.NewGuid()}");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*PostgreSQL*");
    }
}
