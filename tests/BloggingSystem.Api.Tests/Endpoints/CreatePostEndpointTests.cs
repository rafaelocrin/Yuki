using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BloggingSystem.Api.Tests.Fixtures;
using BloggingSystem.Infrastructure.Persistence.Seeding;
using FluentAssertions;

namespace BloggingSystem.Api.Tests.Endpoints;

public sealed class CreatePostEndpointTests : IClassFixture<BloggingApiFactory>
{
    private readonly HttpClient _client;

    public CreatePostEndpointTests(BloggingApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static StringContent Json(object body) =>
        new(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

    [Fact]
    public async Task Post_ValidRequest_Returns201Created()
    {
        var body = new { authorId = DataSeeder.Author1Id, title = "Hello", description = "World", content = "Body" };
        var response = await _client.PostAsync("/post", Json(body));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Post_ValidRequest_LocationHeaderSet()
    {
        var body = new { authorId = DataSeeder.Author1Id, title = "Hello", description = "World", content = "Body" };
        var response = await _client.PostAsync("/post", Json(body));

        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().StartWith("/post/");
    }

    [Fact]
    public async Task Post_ValidRequest_ResponseBodyContainsId()
    {
        var body = new { authorId = DataSeeder.Author2Id, title = "Hello", description = "World", content = "Body" };
        var response = await _client.PostAsync("/post", Json(body));

        var responseBody = await response.Content.ReadFromJsonAsync<JsonElement>();
        responseBody.TryGetProperty("id", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Post_EmptyTitle_Returns400()
    {
        var body = new { authorId = DataSeeder.Author1Id, title = "", description = "World", content = "Body" };
        var response = await _client.PostAsync("/post", Json(body));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_EmptyContent_Returns400()
    {
        var body = new { authorId = DataSeeder.Author1Id, title = "Title", description = "World", content = "" };
        var response = await _client.PostAsync("/post", Json(body));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_UnknownAuthorId_Returns404()
    {
        var body = new { authorId = Guid.NewGuid(), title = "Title", description = "World", content = "Body" };
        var response = await _client.PostAsync("/post", Json(body));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Post_InvalidJsonBody_Returns400()
    {
        var response = await _client.PostAsync("/post",
            new StringContent("not-json", Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_EmptyAuthorId_Returns400()
    {
        var body = new { authorId = Guid.Empty, title = "Title", description = "World", content = "Body" };
        var response = await _client.PostAsync("/post", Json(body));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_InvalidJsonBody_ErrorIsProblemDetails()
    {
        var response = await _client.PostAsync("/post",
            new StringContent("not-json", Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.TryGetProperty("detail", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Post_UnknownAuthorId_ErrorIsProblemDetails()
    {
        var body = new { authorId = Guid.NewGuid(), title = "Title", description = "World", content = "Body" };
        var response = await _client.PostAsync("/post", Json(body));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var responseBody = await response.Content.ReadFromJsonAsync<JsonElement>();
        responseBody.TryGetProperty("detail", out _).Should().BeTrue();
    }
}
