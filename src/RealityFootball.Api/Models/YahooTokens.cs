namespace RealityFootball.Api.Models;

public sealed class YahooTokens
{
    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }
    public required string TokenType { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }

    public bool IsExpired(DateTimeOffset? now = null) =>
        (now ?? DateTimeOffset.UtcNow) >= ExpiresAt.AddMinutes(-1);
}
