using BloggingSystem.Domain.Aggregates.Post;
using BloggingSystem.Application.Commands.CreatePost;
using BloggingSystem.Infrastructure.DependencyInjection;
using BloggingSystem.Api.Endpoints;
using FluentAssertions;
using NetArchTest.Rules;

namespace BloggingSystem.Architecture.Tests;

public sealed class LayerDependencyTests
{
    private const string DomainNs         = "BloggingSystem.Domain";
    private const string ApplicationNs    = "BloggingSystem.Application";
    private const string InfrastructureNs = "BloggingSystem.Infrastructure";
    private const string ApiNs            = "BloggingSystem.Api";

    [Fact]
    public void Domain_ShouldNot_DependOn_Application()
    {
        var result = Types.InAssembly(typeof(Post).Assembly)
            .ShouldNot().HaveDependencyOn(ApplicationNs)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Domain must not depend on {ApplicationNs}. Failing types: {FailingTypes(result)}");
    }

    [Fact]
    public void Domain_ShouldNot_DependOn_Infrastructure()
    {
        var result = Types.InAssembly(typeof(Post).Assembly)
            .ShouldNot().HaveDependencyOn(InfrastructureNs)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Domain must not depend on {InfrastructureNs}. Failing types: {FailingTypes(result)}");
    }

    [Fact]
    public void Domain_ShouldNot_DependOn_Api()
    {
        var result = Types.InAssembly(typeof(Post).Assembly)
            .ShouldNot().HaveDependencyOn(ApiNs)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Domain must not depend on {ApiNs}. Failing types: {FailingTypes(result)}");
    }

    [Fact]
    public void Application_ShouldNot_DependOn_Infrastructure()
    {
        var result = Types.InAssembly(typeof(CreatePostCommand).Assembly)
            .ShouldNot().HaveDependencyOn(InfrastructureNs)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Application must not depend on {InfrastructureNs}. Failing types: {FailingTypes(result)}");
    }

    [Fact]
    public void Application_ShouldNot_DependOn_Api()
    {
        var result = Types.InAssembly(typeof(CreatePostCommand).Assembly)
            .ShouldNot().HaveDependencyOn(ApiNs)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Application must not depend on {ApiNs}. Failing types: {FailingTypes(result)}");
    }

    [Fact]
    public void Infrastructure_ShouldNot_DependOn_Api()
    {
        var result = Types.InAssembly(typeof(InfrastructureServiceExtensions).Assembly)
            .ShouldNot().HaveDependencyOn(ApiNs)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Infrastructure must not depend on {ApiNs}. Failing types: {FailingTypes(result)}");
    }

    [Fact]
    public void Api_ShouldNot_DependOn_Infrastructure_Except_DependencyInjection()
    {
        // Program.cs (composition root) may reference InfrastructureServiceExtensions from
        // BloggingSystem.Infrastructure.DependencyInjection. Every other Infrastructure
        // namespace — Persistence, Time, Serialization — is forbidden from the API layer.
        var forbiddenNamespaces = new[]
        {
            $"{InfrastructureNs}.Persistence",
            $"{InfrastructureNs}.Time",
            $"{InfrastructureNs}.Serialization",
        };

        var result = Types.InAssembly(typeof(CreatePostEndpoint).Assembly)
            .ShouldNot().HaveDependencyOnAny(forbiddenNamespaces)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"API must only reference {InfrastructureNs}.DependencyInjection, " +
                     $"not raw persistence, time, or serialization types. " +
                     $"Failing types: {FailingTypes(result)}");
    }

    private static string FailingTypes(TestResult result) =>
        result.FailingTypes is null ? "none" : string.Join(", ", result.FailingTypes.Select(t => t.FullName));
}
