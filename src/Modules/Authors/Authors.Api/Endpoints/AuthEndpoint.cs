using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Authors.Api.Auth;
using Authors.Api.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using HttpResults = Microsoft.AspNetCore.Http.Results;

namespace Authors.Api.Endpoints;

public static class AuthEndpoint
{
    private static readonly IReadOnlyDictionary<string, (string Password, string Name, Guid Id)> DemoUsers =
        new Dictionary<string, (string, string, Guid)>(StringComparer.OrdinalIgnoreCase)
        {
            ["admin"] = ("admin123", "Admin User", Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")),
            ["author"] = ("author123", "Demo Author", Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"))
        };

    public static void MapAuthEndpoint(this WebApplication app)
    {
        app.MapPost("/auth/token", HandleAsync)
            .WithName("GetToken")
            .WithTags("Auth")
            .WithSummary("Obtain a JWT bearer token")
            .WithDescription(
                "**Demo endpoint** — issues a signed JWT for the given credentials.\n\n" +
                "Built-in demo accounts:\n" +
                "- `admin` / `admin123`\n" +
                "- `author` / `author123`")
            .Accepts<AuthRequest>("application/json")
            .Produces<AuthResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }

    private static IResult HandleAsync(AuthRequest req, IOptions<JwtSettings> jwtOpts)
    {
        if (!DemoUsers.TryGetValue(req.Username, out var user) || user.Password != req.Password)
            return HttpResults.Problem("Invalid username or password.", statusCode: 401, title: "Unauthorized");

        var settings = jwtOpts.Value;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SecretKey));
        var expires = DateTime.UtcNow.AddMinutes(settings.ExpiryMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Role, "author"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return HttpResults.Ok(new AuthResponse(new JwtSecurityTokenHandler().WriteToken(token), expires));
    }
}
