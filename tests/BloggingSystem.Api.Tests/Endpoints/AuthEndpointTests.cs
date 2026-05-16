using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BloggingSystem.Api.Tests.Fixtures;
using FluentAssertions;

namespace BloggingSystem.Api.Tests.Endpoints;

public sealed class AuthEndpointTests : IClassFixture<AnonymousBloggingApiFactory>
{
    private readonly HttpClient _client;

    public AuthEndpointTests(AnonymousBloggingApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static StringContent Json(object body) =>
        new(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

    // ── Token endpoint ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetToken_ValidAdminCredentials_Returns200WithToken()
    {
        var response = await _client.PostAsync("/auth/token",
            Json(new { username = "admin", password = "admin123" }));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.TryGetProperty("token", out var token).Should().BeTrue();
        token.GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetToken_ValidAuthorCredentials_Returns200WithToken()
    {
        var response = await _client.PostAsync("/auth/token",
            Json(new { username = "author", password = "author123" }));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetToken_WrongPassword_Returns401()
    {
        var response = await _client.PostAsync("/auth/token",
            Json(new { username = "admin", password = "wrong" }));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetToken_UnknownUser_Returns401()
    {
        var response = await _client.PostAsync("/auth/token",
            Json(new { username = "nobody", password = "anything" }));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetToken_ResponseContainsExpiresAt()
    {
        var response = await _client.PostAsync("/auth/token",
            Json(new { username = "admin", password = "admin123" }));

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.TryGetProperty("expiresAt", out _).Should().BeTrue();
    }

    // ── Protected endpoints — unauthenticated ─────────────────────────────────

    [Fact]
    public async Task PostPost_WithoutToken_Returns401()
    {
        var response = await _client.PostAsync("/post",
            Json(new { authorId = Guid.NewGuid(), title = "T", description = "D", content = "C" }));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostAuthor_WithoutToken_Returns401()
    {
        var response = await _client.PostAsync("/author",
            Json(new { name = "Alice", surname = "Smith" }));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostPost_WithValidToken_Returns2xx()
    {
        // Obtain a real JWT from the token endpoint.
        var tokenResponse = await _client.PostAsync("/auth/token",
            Json(new { username = "admin", password = "admin123" }));
        var tokenBody = await tokenResponse.Content.ReadFromJsonAsync<JsonElement>();
        var jwt = tokenBody.GetProperty("token").GetString()!;

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);

        var response = await _client.PostAsync("/post",
            Json(new
            {
                authorId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                title = "Auth Test Post",
                description = "Desc",
                content = "Body"
            }));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    // ── Read-only endpoints — no auth required ────────────────────────────────

    [Fact]
    public async Task GetPosts_WithoutToken_Returns200()
    {
        var response = await _client.GetAsync("/post");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
