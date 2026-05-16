using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BloggingSystem.Api.Tests.Fixtures;
using BloggingSystem.Infrastructure.Persistence.Seeding;
using FluentAssertions;

namespace BloggingSystem.Api.Tests.Endpoints;

public sealed class GetPostEndpointTests : IClassFixture<BloggingApiFactory>
{
    private readonly HttpClient _client;

    public GetPostEndpointTests(BloggingApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static StringContent Json(object body) =>
        new(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

    private async Task<Guid> CreatePostAsync(Guid? authorId = null)
    {
        var body = new
        {
            authorId = authorId ?? DataSeeder.Author1Id,
            title = "Test Post",
            description = "Description",
            content = "Content"
        };
        var response = await _client.PostAsync("/post", Json(body));
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task Get_ExistingPost_Returns200()
    {
        var id = await CreatePostAsync();
        var response = await _client.GetAsync($"/post/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_ExistingPost_ReturnsCorrectDto()
    {
        var id = await CreatePostAsync();
        var response = await _client.GetAsync($"/post/{id}");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        body.GetProperty("id").GetGuid().Should().Be(id);
        body.GetProperty("title").GetString().Should().Be("Test Post");
    }

    [Fact]
    public async Task Get_WithIncludeAuthorTrue_ReturnsAuthorData()
    {
        var id = await CreatePostAsync(DataSeeder.Author1Id);
        var response = await _client.GetAsync($"/post/{id}?includeAuthor=true");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var author = body.GetProperty("author");
        author.ValueKind.Should().NotBe(JsonValueKind.Null);
        author.GetProperty("name").GetString().Should().Be("Jane");
    }

    [Fact]
    public async Task Get_WithIncludeAuthorFalse_ReturnsNullAuthor()
    {
        var id = await CreatePostAsync();
        var response = await _client.GetAsync($"/post/{id}?includeAuthor=false");

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("author").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Get_NonExistentId_Returns404()
    {
        var response = await _client.GetAsync($"/post/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_InvalidGuidFormat_Returns400()
    {
        var response = await _client.GetAsync("/post/not-a-guid");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_NonExistentId_ErrorIsProblemDetails()
    {
        var response = await _client.GetAsync($"/post/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.TryGetProperty("detail", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Get_InvalidGuid_ErrorIsProblemDetails()
    {
        var response = await _client.GetAsync("/post/not-a-guid");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.TryGetProperty("detail", out _).Should().BeTrue();
    }
}
