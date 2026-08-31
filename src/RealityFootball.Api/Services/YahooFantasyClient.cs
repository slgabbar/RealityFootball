using System.Net.Http.Headers;

namespace RealityFootball.Api.Services;

public sealed class YahooFantasyClient(
    IHttpClientFactory httpClientFactory,
    YahooOAuthService oauthService,
    ILogger<YahooFantasyClient> logger)
{
    private const string FantasyBaseUrl = "https://fantasysports.yahooapis.com/fantasy/v2";

    public async Task<string> GetCurrentUserGamesJsonAsync(CancellationToken cancellationToken = default)
    {
        var accessToken = await oauthService.GetValidAccessTokenAsync(cancellationToken)
            ?? throw new InvalidOperationException("Not authenticated with Yahoo. Visit /auth/login first.");

        var requestUri = $"{FantasyBaseUrl}/users;use_login=1/games?format=json";
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var client = httpClientFactory.CreateClient(nameof(YahooFantasyClient));
        using var response = await client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Yahoo Fantasy API request failed: {StatusCode} {Body}", response.StatusCode, body);
            throw new InvalidOperationException($"Yahoo Fantasy API request failed: {response.StatusCode}");
        }

        return body;
    }
}
