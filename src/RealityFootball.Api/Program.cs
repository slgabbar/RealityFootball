using Microsoft.Extensions.Options;
using RealityFootball.Api.Mcp;
using RealityFootball.Api.Options;
using RealityFootball.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.Configure<YahooOptions>(builder.Configuration.GetSection(YahooOptions.SectionName));
builder.Services.AddHttpClient(nameof(YahooOAuthService));
builder.Services.AddHttpClient(nameof(YahooFantasyClient));
builder.Services.AddSingleton<YahooTokenStore>();
builder.Services.AddSingleton<YahooOAuthService>();
builder.Services.AddSingleton<YahooFantasyClient>();

builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithTools<YahooFantasyTools>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/", () => Results.Ok(new
{
    message = "RealityFootball API + MCP",
    auth = "/auth/login",
    status = "/auth/status",
    mcp = "/mcp",
    yahoo = new
    {
        games = "/yahoo/games",
        leagues = "/yahoo/leagues",
        teams = "/yahoo/teams",
        standings = "/yahoo/leagues/{leagueKey}/standings",
        leagueTeams = "/yahoo/leagues/{leagueKey}/teams",
        roster = "/yahoo/teams/{teamKey}/roster",
        freeAgents = "/yahoo/leagues/{leagueKey}/free-agents",
        scoreboard = "/yahoo/leagues/{leagueKey}/scoreboard",
    },
}));

app.MapGet("/auth/login", (HttpContext context, YahooOAuthService oauthService, IOptions<YahooOptions> options) =>
{
    var yahooOptions = options.Value;
    if (string.IsNullOrWhiteSpace(yahooOptions.ClientId) || string.IsNullOrWhiteSpace(yahooOptions.RedirectUri))
    {
        return Results.Problem(
            "Yahoo OAuth is not configured. Set Yahoo:ClientId, Yahoo:ClientSecret, and Yahoo:RedirectUri via user-secrets.",
            statusCode: StatusCodes.Status500InternalServerError);
    }

    var state = Guid.NewGuid().ToString("N");
    context.Response.Cookies.Append("yahoo_oauth_state", state, new CookieOptions
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Lax,
        MaxAge = TimeSpan.FromMinutes(10),
    });

    return Results.Redirect(oauthService.BuildAuthorizationUrl(state));
});

app.MapGet("/auth/callback", async (
    HttpContext context,
    YahooOAuthService oauthService,
    string? code,
    string? state,
    string? error,
    CancellationToken cancellationToken) =>
{
    if (!string.IsNullOrWhiteSpace(error))
    {
        return Results.Problem($"Yahoo authorization failed: {error}", statusCode: StatusCodes.Status400BadRequest);
    }

    if (string.IsNullOrWhiteSpace(code))
    {
        return Results.BadRequest("Missing authorization code.");
    }

    if (!context.Request.Cookies.TryGetValue("yahoo_oauth_state", out var expectedState)
        || string.IsNullOrWhiteSpace(state)
        || !string.Equals(expectedState, state, StringComparison.Ordinal))
    {
        return Results.BadRequest("Invalid OAuth state.");
    }

    context.Response.Cookies.Delete("yahoo_oauth_state");

    try
    {
        await oauthService.ExchangeCodeAsync(code, cancellationToken);
        return Results.Ok(new
        {
            message = "Yahoo authentication succeeded.",
            next = "/yahoo/leagues",
        });
    }
    catch (InvalidOperationException ex)
    {
        return Results.Problem(ex.Message, statusCode: StatusCodes.Status502BadGateway);
    }
});

app.MapGet("/auth/status", async (YahooTokenStore tokenStore, CancellationToken cancellationToken) =>
{
    var tokens = await tokenStore.GetAsync(cancellationToken);
    if (tokens is null)
    {
        return Results.Ok(new { authenticated = false });
    }

    return Results.Ok(new
    {
        authenticated = true,
        expiresAt = tokens.ExpiresAt,
        expired = tokens.IsExpired(),
    });
});

app.MapGet("/auth/logout", async (YahooTokenStore tokenStore, CancellationToken cancellationToken) =>
{
    await tokenStore.ClearAsync(cancellationToken);
    return Results.Ok(new { message = "Yahoo tokens cleared." });
});

app.MapGet("/yahoo/games", (YahooFantasyClient fantasyClient, CancellationToken cancellationToken) =>
    YahooJson(fantasyClient.GetCurrentUserGamesJsonAsync(cancellationToken)));

app.MapGet("/yahoo/leagues", (YahooFantasyClient fantasyClient, CancellationToken cancellationToken) =>
    YahooJson(fantasyClient.GetMyLeaguesAsync(cancellationToken)));

app.MapGet("/yahoo/teams", (YahooFantasyClient fantasyClient, CancellationToken cancellationToken) =>
    YahooJson(fantasyClient.GetMyTeamsAsync(cancellationToken)));

app.MapGet("/yahoo/leagues/{leagueKey}", (string leagueKey, YahooFantasyClient fantasyClient, CancellationToken cancellationToken) =>
    YahooJson(fantasyClient.GetLeagueAsync(leagueKey, cancellationToken)));

app.MapGet("/yahoo/leagues/{leagueKey}/standings", (string leagueKey, YahooFantasyClient fantasyClient, CancellationToken cancellationToken) =>
    YahooJson(fantasyClient.GetLeagueStandingsAsync(leagueKey, cancellationToken)));

app.MapGet("/yahoo/leagues/{leagueKey}/teams", (string leagueKey, YahooFantasyClient fantasyClient, CancellationToken cancellationToken) =>
    YahooJson(fantasyClient.GetLeagueTeamsAsync(leagueKey, cancellationToken)));

app.MapGet("/yahoo/leagues/{leagueKey}/free-agents", (
    string leagueKey,
    YahooFantasyClient fantasyClient,
    string? position,
    int? start,
    int? count,
    CancellationToken cancellationToken) =>
    YahooJson(fantasyClient.GetFreeAgentsAsync(leagueKey, position, start ?? 0, count ?? 25, cancellationToken)));

app.MapGet("/yahoo/leagues/{leagueKey}/scoreboard", (
    string leagueKey,
    YahooFantasyClient fantasyClient,
    int? week,
    CancellationToken cancellationToken) =>
    YahooJson(fantasyClient.GetLeagueScoreboardAsync(leagueKey, week, cancellationToken)));

app.MapGet("/yahoo/teams/{teamKey}", (string teamKey, YahooFantasyClient fantasyClient, CancellationToken cancellationToken) =>
    YahooJson(fantasyClient.GetTeamAsync(teamKey, cancellationToken)));

app.MapGet("/yahoo/teams/{teamKey}/roster", (
    string teamKey,
    YahooFantasyClient fantasyClient,
    int? week,
    CancellationToken cancellationToken) =>
    YahooJson(fantasyClient.GetTeamRosterAsync(teamKey, week, cancellationToken)));

app.MapMcp("/mcp");

app.Run();

static async Task<IResult> YahooJson(Task<string> jsonTask)
{
    try
    {
        var json = await jsonTask;
        return Results.Content(json, "application/json");
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("Not authenticated", StringComparison.OrdinalIgnoreCase))
    {
        return Results.Problem(ex.Message, statusCode: StatusCodes.Status401Unauthorized);
    }
    catch (InvalidOperationException ex)
    {
        return Results.Problem(ex.Message, statusCode: StatusCodes.Status502BadGateway);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
}
