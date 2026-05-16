using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BloggingSystem.Api.Tests.Fixtures;
using FluentAssertions;

namespace BloggingSystem.Api.Tests.Endpoints;

public sealed class CreateAuthorEndpointTests : IClassFixture<BloggingApiFactory>
{
    private readonly HttpClient _client;

    public CreateAuthorEndpointTests(BloggingApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static StringContent Json(object body) =>
        new(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

    [Fact]
    public async Task Post_ValidAuthor_Returns201WithLocationHeader()
    {
        var body = new { name = "Alice", surname = "Jones" };
        var response = await _client.PostAsync("/author", Json(body));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().StartWith("/author/");

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("id").GetGuid().Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Post_EmptyName_ReturnsProblemDetails400()
    {
        var body = new { name = "", surname = "Jones" };
        var response = await _client.PostAsync("/author", Json(body));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("title").GetString().Should().Be("Validation Failed");
        json.TryGetProperty("errors", out var errors).Should().BeTrue();
        errors.TryGetProperty("Name", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Post_EmptySurname_ReturnsProblemDetails400()
    {
        var body = new { name = "Alice", surname = "" };
        var response = await _client.PostAsync("/author", Json(body));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("title").GetString().Should().Be("Validation Failed");
        json.TryGetProperty("errors", out var errors).Should().BeTrue();
        errors.TryGetProperty("Surname", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Post_TwoAuthors_ReturnDistinctIds()
    {
        var body = new { name = "Bob", surname = "Smith" };
        var r1 = await _client.PostAsync("/author", Json(body));
        var r2 = await _client.PostAsync("/author", Json(body));

        r1.StatusCode.Should().Be(HttpStatusCode.Created);
        r2.StatusCode.Should().Be(HttpStatusCode.Created);

        var id1 = (await r1.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var id2 = (await r2.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        id1.Should().NotBe(id2);
    }
}
