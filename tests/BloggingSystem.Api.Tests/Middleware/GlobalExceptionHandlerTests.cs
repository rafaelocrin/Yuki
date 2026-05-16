using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BloggingSystem.Api.Tests.Fixtures;
using BloggingSystem.Infrastructure.Persistence.Seeding;
using FluentAssertions;

namespace BloggingSystem.Api.Tests.Middleware;

public sealed class GlobalExceptionHandlerTests : IClassFixture<BloggingApiFactory>
{
    private readonly HttpClient _client;

    public GlobalExceptionHandlerTests(BloggingApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static StringContent Json(object body) =>
        new(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

    [Fact]
    public async Task ValidationException_EmptyTitle_ReturnsProblemDetails400()
    {
        var body = new { authorId = DataSeeder.Author1Id, title = "", description = "D", content = "Body" };
        var response = await _client.PostAsync("/post", Json(body));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("title").GetString().Should().Be("Validation Failed");
        json.TryGetProperty("errors", out var errors).Should().BeTrue();
        errors.TryGetProperty("Title", out _).Should().BeTrue();
    }

    [Fact]
    public async Task ValidationException_EmptyContent_ReturnsProblemDetails400()
    {
        var body = new { authorId = DataSeeder.Author1Id, title = "Title", description = "D", content = "" };
        var response = await _client.PostAsync("/post", Json(body));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("title").GetString().Should().Be("Validation Failed");
    }

    [Fact]
    public async Task ValidationException_EmptyAuthorId_ReturnsProblemDetails400()
    {
        var body = new { authorId = Guid.Empty, title = "Title", description = "D", content = "Body" };
        var response = await _client.PostAsync("/post", Json(body));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task PostNotFoundException_ReturnsProblemDetails404()
    {
        var response = await _client.GetAsync($"/post/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("title").GetString().Should().Be("Not Found");
        json.TryGetProperty("detail", out _).Should().BeTrue();
    }

    [Fact]
    public async Task AuthorNotFoundException_ReturnsProblemDetails404()
    {
        var body = new { authorId = Guid.NewGuid(), title = "Title", description = "D", content = "Body" };
        var response = await _client.PostAsync("/post", Json(body));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("title").GetString().Should().Be("Not Found");
    }
}
