using Posts.Domain.Aggregates;
using Posts.Application.Commands.CreatePost;
using Posts.Infrastructure.DependencyInjection;
using Authors.Domain.Aggregates;
using Authors.Application.Commands.CreateAuthor;
using Authors.Infrastructure.DependencyInjection;
using Authors.Contracts;
using Shared.Domain.Events;
using Shared.Application.Ports;
using Shared.Infrastructure.EventStore;
using BloggingSystem.Api.Endpoints;
using FluentAssertions;
using NetArchTest.Rules;

namespace BloggingSystem.Architecture.Tests;

public sealed class LayerDependencyTests
{
    // Namespace roots
    private const string PostsDomainNs         = "Posts.Domain";
    private const string PostsApplicationNs    = "Posts.Application";
    private const string PostsInfrastructureNs = "Posts.Infrastructure";

    private const string AuthorsContractsNs       = "Authors.Contracts";
    private const string AuthorsDomainNs          = "Authors.Domain";
    private const string AuthorsApplicationNs     = "Authors.Application";
    private const string AuthorsInfrastructureNs  = "Authors.Infrastructure";

    private const string SharedDomainNs         = "Shared.Domain";
    private const string SharedApplicationNs    = "Shared.Application";
    private const string SharedInfrastructureNs = "Shared.Infrastructure";

    private const string ApiNs = "BloggingSystem.Api";

    // ─── Domain isolation ────────────────────────────────────────────────────

    [Fact]
    public void PostsDomain_ShouldNot_DependOn_Application()
    {
        var result = Types.InAssembly(typeof(Post).Assembly)
            .ShouldNot().HaveDependencyOnAny(PostsApplicationNs, AuthorsApplicationNs, SharedApplicationNs)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Posts.Domain must not depend on Application layers. Failing: {FailingTypes(result)}");
    }

    [Fact]
    public void PostsDomain_ShouldNot_DependOn_Infrastructure()
    {
        var result = Types.InAssembly(typeof(Post).Assembly)
            .ShouldNot().HaveDependencyOnAny(PostsInfrastructureNs, AuthorsInfrastructureNs, SharedInfrastructureNs)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Posts.Domain must not depend on Infrastructure layers. Failing: {FailingTypes(result)}");
    }

    [Fact]
    public void PostsDomain_ShouldNot_DependOn_Api()
    {
        var result = Types.InAssembly(typeof(Post).Assembly)
            .ShouldNot().HaveDependencyOn(ApiNs)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Posts.Domain must not depend on {ApiNs}. Failing: {FailingTypes(result)}");
    }

    [Fact]
    public void PostsDomain_ShouldNot_DependOn_AuthorsDomain()
    {
        var result = Types.InAssembly(typeof(Post).Assembly)
            .ShouldNot().HaveDependencyOn(AuthorsDomainNs)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Posts.Domain must not depend on Authors.Domain. Failing: {FailingTypes(result)}");
    }

    [Fact]
    public void AuthorsDomain_ShouldNot_DependOn_Application()
    {
        var result = Types.InAssembly(typeof(Author).Assembly)
            .ShouldNot().HaveDependencyOnAny(PostsApplicationNs, AuthorsApplicationNs, SharedApplicationNs)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Authors.Domain must not depend on Application layers. Failing: {FailingTypes(result)}");
    }

    [Fact]
    public void AuthorsDomain_ShouldNot_DependOn_Infrastructure()
    {
        var result = Types.InAssembly(typeof(Author).Assembly)
            .ShouldNot().HaveDependencyOnAny(PostsInfrastructureNs, AuthorsInfrastructureNs, SharedInfrastructureNs)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Authors.Domain must not depend on Infrastructure layers. Failing: {FailingTypes(result)}");
    }

    [Fact]
    public void AuthorsDomain_ShouldNot_DependOn_PostsDomain()
    {
        var result = Types.InAssembly(typeof(Author).Assembly)
            .ShouldNot().HaveDependencyOn(PostsDomainNs)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Authors.Domain must not depend on Posts.Domain. Failing: {FailingTypes(result)}");
    }

    [Fact]
    public void SharedDomain_ShouldNot_DependOn_Anything()
    {
        var result = Types.InAssembly(typeof(IDomainEvent).Assembly)
            .ShouldNot().HaveDependencyOnAny(
                PostsDomainNs, PostsApplicationNs, PostsInfrastructureNs,
                AuthorsContractsNs, AuthorsDomainNs, AuthorsApplicationNs, AuthorsInfrastructureNs,
                SharedApplicationNs, SharedInfrastructureNs, ApiNs)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Shared.Domain must have zero dependencies on other modules. Failing: {FailingTypes(result)}");
    }

    // ─── Application isolation ───────────────────────────────────────────────

    [Fact]
    public void PostsApplication_ShouldNot_DependOn_Infrastructure()
    {
        var result = Types.InAssembly(typeof(CreatePostCommand).Assembly)
            .ShouldNot().HaveDependencyOnAny(PostsInfrastructureNs, AuthorsInfrastructureNs, SharedInfrastructureNs)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Posts.Application must not depend on Infrastructure. Failing: {FailingTypes(result)}");
    }

    [Fact]
    public void PostsApplication_ShouldNot_DependOn_Api()
    {
        var result = Types.InAssembly(typeof(CreatePostCommand).Assembly)
            .ShouldNot().HaveDependencyOn(ApiNs)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Posts.Application must not depend on {ApiNs}. Failing: {FailingTypes(result)}");
    }

    [Fact]
    public void PostsApplication_ShouldNot_DependOn_AuthorsApplication()
    {
        var result = Types.InAssembly(typeof(CreatePostCommand).Assembly)
            .ShouldNot().HaveDependencyOn(AuthorsApplicationNs)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Posts.Application must not depend on Authors.Application. Failing: {FailingTypes(result)}");
    }

    [Fact]
    public void AuthorsApplication_ShouldNot_DependOn_Infrastructure()
    {
        var result = Types.InAssembly(typeof(CreateAuthorCommand).Assembly)
            .ShouldNot().HaveDependencyOnAny(PostsInfrastructureNs, AuthorsInfrastructureNs, SharedInfrastructureNs)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Authors.Application must not depend on Infrastructure. Failing: {FailingTypes(result)}");
    }

    [Fact]
    public void AuthorsApplication_ShouldNot_DependOn_Api()
    {
        var result = Types.InAssembly(typeof(CreateAuthorCommand).Assembly)
            .ShouldNot().HaveDependencyOn(ApiNs)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Authors.Application must not depend on {ApiNs}. Failing: {FailingTypes(result)}");
    }

    [Fact]
    public void AuthorsApplication_ShouldNot_DependOn_PostsApplication()
    {
        var result = Types.InAssembly(typeof(CreateAuthorCommand).Assembly)
            .ShouldNot().HaveDependencyOn(PostsApplicationNs)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Authors.Application must not depend on Posts.Application. Failing: {FailingTypes(result)}");
    }

    [Fact]
    public void SharedApplication_ShouldNot_DependOn_Infrastructure()
    {
        var result = Types.InAssembly(typeof(IEventStore).Assembly)
            .ShouldNot().HaveDependencyOnAny(PostsInfrastructureNs, AuthorsInfrastructureNs, SharedInfrastructureNs)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Shared.Application must not depend on Infrastructure. Failing: {FailingTypes(result)}");
    }

    [Fact]
    public void SharedApplication_ShouldNot_DependOn_Api()
    {
        var result = Types.InAssembly(typeof(IEventStore).Assembly)
            .ShouldNot().HaveDependencyOn(ApiNs)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Shared.Application must not depend on {ApiNs}. Failing: {FailingTypes(result)}");
    }

    // ─── Infrastructure isolation ────────────────────────────────────────────

    [Fact]
    public void PostsInfrastructure_ShouldNot_DependOn_Api()
    {
        var result = Types.InAssembly(typeof(PostsModuleExtensions).Assembly)
            .ShouldNot().HaveDependencyOn(ApiNs)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Posts.Infrastructure must not depend on {ApiNs}. Failing: {FailingTypes(result)}");
    }

    [Fact]
    public void PostsInfrastructure_ShouldNot_DependOn_AuthorsInfrastructure()
    {
        var result = Types.InAssembly(typeof(PostsModuleExtensions).Assembly)
            .ShouldNot().HaveDependencyOn(AuthorsInfrastructureNs)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Posts.Infrastructure must not depend on Authors.Infrastructure. Failing: {FailingTypes(result)}");
    }

    [Fact]
    public void AuthorsInfrastructure_ShouldNot_DependOn_Api()
    {
        var result = Types.InAssembly(typeof(AuthorsModuleExtensions).Assembly)
            .ShouldNot().HaveDependencyOn(ApiNs)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Authors.Infrastructure must not depend on {ApiNs}. Failing: {FailingTypes(result)}");
    }

    [Fact]
    public void AuthorsInfrastructure_ShouldNot_DependOn_PostsInfrastructure()
    {
        var result = Types.InAssembly(typeof(AuthorsModuleExtensions).Assembly)
            .ShouldNot().HaveDependencyOn(PostsInfrastructureNs)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Authors.Infrastructure must not depend on Posts.Infrastructure. Failing: {FailingTypes(result)}");
    }

    [Fact]
    public void SharedInfrastructure_ShouldNot_DependOn_Api()
    {
        var result = Types.InAssembly(typeof(InMemoryEventStore).Assembly)
            .ShouldNot().HaveDependencyOn(ApiNs)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Shared.Infrastructure must not depend on {ApiNs}. Failing: {FailingTypes(result)}");
    }

    // ─── API isolation ───────────────────────────────────────────────────────

    [Fact]
    public void Api_ShouldNot_DependOn_RawInfrastructureInternals()
    {
        // Program.cs (composition root) may reference extension methods from
        // *.Infrastructure.DependencyInjection. Every other Infrastructure
        // namespace — Persistence, Time, Serialization, EventStore, Outbox, Seeding
        // — is forbidden from the API layer.
        var forbiddenNamespaces = new[]
        {
            $"{PostsInfrastructureNs}.Persistence",
            $"{PostsInfrastructureNs}.Outbox",
            $"{AuthorsInfrastructureNs}.Persistence",
            $"{AuthorsInfrastructureNs}.Seeding",
            $"{SharedInfrastructureNs}.EventStore",
            $"{SharedInfrastructureNs}.Serialization",
            $"{SharedInfrastructureNs}.Time",
        };

        var result = Types.InAssembly(typeof(CreatePostEndpoint).Assembly)
            .ShouldNot().HaveDependencyOnAny(forbiddenNamespaces)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"API must only reference *.Infrastructure.DependencyInjection, " +
                     $"not raw persistence, time, or serialization types. " +
                     $"Failing types: {FailingTypes(result)}");
    }

    // ─── Contracts isolation ─────────────────────────────────────────────────

    [Fact]
    public void AuthorsContracts_ShouldNot_DependOn_ApplicationOrInfrastructure()
    {
        var result = Types.InAssembly(typeof(AuthorCreatedEvent).Assembly)
            .ShouldNot().HaveDependencyOnAny(
                PostsDomainNs, PostsApplicationNs, PostsInfrastructureNs,
                AuthorsDomainNs, AuthorsApplicationNs, AuthorsInfrastructureNs,
                SharedApplicationNs, SharedInfrastructureNs, ApiNs)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Authors.Contracts must depend only on Shared.Domain. Failing: {FailingTypes(result)}");
    }

    // ─── Helper ─────────────────────────────────────────────────────────────

    private static string FailingTypes(TestResult result) =>
        result.FailingTypes is null ? "none" : string.Join(", ", result.FailingTypes.Select(t => t.FullName));
}
