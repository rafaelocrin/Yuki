namespace BloggingSystem.Api.Models;

public sealed record AuthResponse(string Token, DateTime ExpiresAt);
