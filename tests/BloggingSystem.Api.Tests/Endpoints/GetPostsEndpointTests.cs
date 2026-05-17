using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BloggingSystem.Api.Tests.Fixtures;
using Authors.Infrastructure.Seeding;
using FluentAssertions;

namespace BloggingSystem.Api.Tests.Endpoints;

[Trait("Category", "Functional")]
public sealed class GetPostsEndpointTests : IClassFixture<BloggingApiFactory>
{
    private readonly HttpClient _client;

    public GetPostsEndpointTests(BloggingApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static StringContent Json(object body) =>
        new(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

    private async Task CreatePostAsync(string title = "Test Post")
    {
        var body = new { authorId = AuthorSeeder.Author1Id, title, description = "Desc", content = "Body" };
        var response = await _client.PostAsync("/post", Json(body));
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetPosts_Returns200WithCorrectPagedShape()
    {
        var response = await _client.GetAsync("/post");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.TryGetProperty("items", out _).Should().BeTrue();
        body.TryGetProperty("totalCount", out _).Should().BeTrue();
        body.GetProperty("page").GetInt32().Should().Be(1);
        body.GetProperty("pageSize").GetInt32().Should().Be(10);
        body.TryGetProperty("totalPages", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GetPosts_WithPosts_ReturnsItems()
    {
        await CreatePostAsync("Alpha");
        await CreatePostAsync("Beta");

        var response = await _client.GetAsync("/post");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("totalCount").GetInt32().Should().BeGreaterThanOrEqualTo(2);
        body.GetProperty("items").GetArrayLength().Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetPosts_DefaultPageSize_Returns10OrFewer()
    {
        var response = await _client.GetAsync("/post");

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("items").GetArrayLength().Should().BeLessThanOrEqualTo(10);
        body.GetProperty("pageSize").GetInt32().Should().Be(10);
        body.GetProperty("page").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task GetPosts_WithPageSize1_ReturnsSingleItem()
    {
        await CreatePostAsync("PagedPost");

        var response = await _client.GetAsync("/post?page=1&pageSize=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("items").GetArrayLength().Should().Be(1);
        body.GetProperty("pageSize").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task GetPosts_TotalPagesCalculated()
    {
        var response = await _client.GetAsync("/post?pageSize=1");

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var totalCount = body.GetProperty("totalCount").GetInt32();
        var totalPages = body.GetProperty("totalPages").GetInt32();
        totalPages.Should().Be((int)Math.Ceiling((double)totalCount / 1));
    }

    [Fact]
    public async Task GetPosts_InvalidPage_Returns400()
    {
        var response = await _client.GetAsync("/post?page=0");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetPosts_PageSizeOver100_Returns400()
    {
        var response = await _client.GetAsync("/post?pageSize=101");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetPosts_WithIncludeAuthorTrue_ReturnsAuthorInItems()
    {
        await CreatePostAsync("AuthorPost");

        var response = await _client.GetAsync("/post?includeAuthor=true");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var items = body.GetProperty("items");
        items.GetArrayLength().Should().BeGreaterThan(0);
        items[0].GetProperty("author").ValueKind.Should().NotBe(JsonValueKind.Null);
    }
}
