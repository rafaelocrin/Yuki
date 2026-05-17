using System.Net;
using System.Text.Json;
using BloggingSystem.Api.Tests.Fixtures;
using FluentAssertions;

namespace BloggingSystem.Api.Tests.Endpoints;

[Trait("Category", "Functional")]
public sealed class HealthCheckEndpointTests : IClassFixture<BloggingApiFactory>
{
    private readonly HttpClient _client;

    public HealthCheckEndpointTests(BloggingApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Live_Returns200()
    {
        var response = await _client.GetAsync("/healthz/live");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Live_ReturnsJsonWithHealthyStatus()
    {
        var response = await _client.GetAsync("/healthz/live");

        var body = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(body);
        json.GetProperty("status").GetString().Should().Be("Healthy");
    }

    [Fact]
    public async Task Ready_Returns200_WhenInMemoryProviders()
    {
        // InMemory providers have no PostgreSQL probe, so ready = Healthy with no checks.
        var response = await _client.GetAsync("/healthz/ready");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Live_ResponseContainsXCorrelationIdHeader()
    {
        var response = await _client.GetAsync("/healthz/live");

        response.Headers.Should().ContainKey("X-Correlation-ID");
    }
}
