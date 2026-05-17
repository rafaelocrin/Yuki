namespace Authors.Api.Models;

public sealed record AuthResponse(string Token, DateTime ExpiresAt);
