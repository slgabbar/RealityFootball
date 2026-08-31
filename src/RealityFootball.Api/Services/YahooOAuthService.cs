using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using RealityFootball.Api.Models;
using RealityFootball.Api.Options;

namespace RealityFootball.Api.Services;

public sealed class YahooOAuthService(
    IOptions<YahooOptions> options,
    IHttpClientFactory httpClientFactory,
    YahooTokenStore tokenStore,
    ILogger<YahooOAuthService> logger)
{
    private const string AuthorizeUrl = "https://api.login.yahoo.com/oauth2/request_auth";
    private const string TokenUrl = "https://api.login.yahoo.com/oauth2/get_token";

    private readonly YahooOptions _options = options.Value;

    public string BuildAuthorizationUrl(string state)
    {
        // Yahoo's auth-code flow does not take a scope query param.
        // Fantasy Sports access is granted from the Developer app permissions.
        var query = new Dictionary<string, string?>
        {
            ["client_id"] = _options.ClientId,
            ["redirect_uri"] = _options.RedirectUri,
            ["response_type"] = "code",
            ["state"] = state,
        };

        return QueryHelpers.AddQueryString(AuthorizeUrl, query);
    }

    public async Task<YahooTokens> ExchangeCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var payload = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["redirect_uri"] = _options.RedirectUri,
            ["code"] = code,
        };

        var tokens = await RequestTokensAsync(payload, cancellationToken);
        await tokenStore.SaveAsync(tokens, cancellationToken);
        return tokens;
    }

    public async Task<string?> GetValidAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        var tokens = await tokenStore.GetAsync(cancellationToken);
        if (tokens is null)
        {
            return null;
        }

        if (!tokens.IsExpired())
        {
            return tokens.AccessToken;
        }

        logger.LogInformation("Yahoo access token expired; refreshing.");
        var refreshed = await RefreshAsync(tokens.RefreshToken, cancellationToken);
        return refreshed.AccessToken;
    }

    private async Task<YahooTokens> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var payload = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
        };

        var tokens = await RequestTokensAsync(payload, cancellationToken);
        await tokenStore.SaveAsync(tokens, cancellationToken);
        return tokens;
    }

    private async Task<YahooTokens> RequestTokensAsync(
        Dictionary<string, string> payload,
        CancellationToken cancellationToken)
    {
        ValidateConfiguration();

        using var request = new HttpRequestMessage(HttpMethod.Post, TokenUrl);
        request.Headers.Authorization = CreateBasicAuthHeader(_options.ClientId, _options.ClientSecret);
        request.Content = new FormUrlEncodedContent(payload);

        var client = httpClientFactory.CreateClient(nameof(YahooOAuthService));
        using var response = await client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Yahoo token request failed: {StatusCode} {Body}", response.StatusCode, body);
            throw new InvalidOperationException($"Yahoo token request failed: {response.StatusCode}");
        }

        var tokenResponse = JsonSerializer.Deserialize<YahooTokenResponse>(body)
            ?? throw new InvalidOperationException("Yahoo token response was empty.");

        if (string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
        {
            throw new InvalidOperationException("Yahoo token response did not include an access token.");
        }

        var refreshToken = tokenResponse.RefreshToken
            ?? (await tokenStore.GetAsync(cancellationToken))?.RefreshToken
            ?? throw new InvalidOperationException("Yahoo token response did not include a refresh token.");

        return new YahooTokens
        {
            AccessToken = tokenResponse.AccessToken,
            RefreshToken = refreshToken,
            TokenType = tokenResponse.TokenType ?? "bearer",
            ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn),
        };
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_options.ClientId)
            || string.IsNullOrWhiteSpace(_options.ClientSecret)
            || string.IsNullOrWhiteSpace(_options.RedirectUri))
        {
            throw new InvalidOperationException(
                "Yahoo OAuth is not configured. Set Yahoo:ClientId, Yahoo:ClientSecret, and Yahoo:RedirectUri.");
        }
    }

    private static AuthenticationHeaderValue CreateBasicAuthHeader(string clientId, string clientSecret)
    {
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
        return new AuthenticationHeaderValue("Basic", credentials);
    }

    private sealed class YahooTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; init; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; init; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; init; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; init; }
    }
}
