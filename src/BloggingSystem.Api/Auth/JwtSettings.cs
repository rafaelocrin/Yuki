namespace BloggingSystem.Api.Auth;

public sealed class JwtSettings
{
    public string Issuer { get; init; } = "";
    public string Audience { get; init; } = "";
    public string SecretKey { get; init; } = "";
    public int ExpiryMinutes { get; init; } = 60;
}
