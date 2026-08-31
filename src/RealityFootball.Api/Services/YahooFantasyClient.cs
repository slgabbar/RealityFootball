using System.Net.Http.Headers;

namespace RealityFootball.Api.Services;

public sealed class YahooFantasyClient(
    IHttpClientFactory httpClientFactory,
    YahooOAuthService oauthService,
    ILogger<YahooFantasyClient> logger)
{
    private const string FantasyBaseUrl = "https://fantasysports.yahooapis.com/fantasy/v2";

    /// <summary>Games the logged-in user belongs to (e.g. NFL seasons).</summary>
    public Task<string> GetCurrentUserGamesJsonAsync(CancellationToken cancellationToken = default) =>
        GetAsync("users;use_login=1/games", cancellationToken);

    /// <summary>Leagues the logged-in user belongs to across games.</summary>
    public Task<string> GetMyLeaguesAsync(CancellationToken cancellationToken = default) =>
        GetAsync("users;use_login=1/games/leagues", cancellationToken);

    /// <summary>Teams owned by the logged-in user.</summary>
    public Task<string> GetMyTeamsAsync(CancellationToken cancellationToken = default) =>
        GetAsync("users;use_login=1/games/teams", cancellationToken);

    /// <summary>League metadata (name, week, settings basics).</summary>
    public Task<string> GetLeagueAsync(string leagueKey, CancellationToken cancellationToken = default) =>
        GetAsync($"league/{EncodeKey(leagueKey)}", cancellationToken);

    /// <summary>League standings.</summary>
    public Task<string> GetLeagueStandingsAsync(string leagueKey, CancellationToken cancellationToken = default) =>
        GetAsync($"league/{EncodeKey(leagueKey)}/standings", cancellationToken);

    /// <summary>All teams in a league.</summary>
    public Task<string> GetLeagueTeamsAsync(string leagueKey, CancellationToken cancellationToken = default) =>
        GetAsync($"league/{EncodeKey(leagueKey)}/teams", cancellationToken);

    /// <summary>Team metadata.</summary>
    public Task<string> GetTeamAsync(string teamKey, CancellationToken cancellationToken = default) =>
        GetAsync($"team/{EncodeKey(teamKey)}", cancellationToken);

    /// <summary>Team roster for current week, or a specific week.</summary>
    public Task<string> GetTeamRosterAsync(
        string teamKey,
        int? week = null,
        CancellationToken cancellationToken = default)
    {
        var path = week is null
            ? $"team/{EncodeKey(teamKey)}/roster"
            : $"team/{EncodeKey(teamKey)}/roster;week={week.Value}";
        return GetAsync(path, cancellationToken);
    }

    /// <summary>Available / free-agent players in a league.</summary>
    public Task<string> GetFreeAgentsAsync(
        string leagueKey,
        string? position = null,
        int start = 0,
        int count = 25,
        CancellationToken cancellationToken = default)
    {
        var path = $"league/{EncodeKey(leagueKey)}/players;status=A;start={start};count={count}";
        if (!string.IsNullOrWhiteSpace(position))
        {
            path += $";position={Uri.EscapeDataString(position.Trim().ToUpperInvariant())}";
        }

        return GetAsync(path, cancellationToken);
    }

    /// <summary>League scoreboard / matchups for current or specific week.</summary>
    public Task<string> GetLeagueScoreboardAsync(
        string leagueKey,
        int? week = null,
        CancellationToken cancellationToken = default)
    {
        var path = week is null
            ? $"league/{EncodeKey(leagueKey)}/scoreboard"
            : $"league/{EncodeKey(leagueKey)}/scoreboard;week={week.Value}";
        return GetAsync(path, cancellationToken);
    }

    private async Task<string> GetAsync(string relativePath, CancellationToken cancellationToken)
    {
        var accessToken = await oauthService.GetValidAccessTokenAsync(cancellationToken)
            ?? throw new InvalidOperationException("Not authenticated with Yahoo. Visit /auth/login first.");

        var requestUri = $"{FantasyBaseUrl}/{relativePath.TrimStart('/')}?format=json";
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var client = httpClientFactory.CreateClient(nameof(YahooFantasyClient));
        using var response = await client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError(
                "Yahoo Fantasy API request failed for {Path}: {StatusCode} {Body}",
                relativePath,
                response.StatusCode,
                body);
            throw new InvalidOperationException($"Yahoo Fantasy API request failed: {response.StatusCode}");
        }

        return body;
    }

    private static string EncodeKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key is required.", nameof(key));
        }

        // Yahoo keys look like "461.l.12345" / "461.l.12345.t.1" — leave as path segments.
        return key.Trim();
    }
}
