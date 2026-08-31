using System.ComponentModel;
using ModelContextProtocol.Server;
using RealityFootball.Api.Services;

namespace RealityFootball.Api.Mcp;

[McpServerToolType]
public sealed class YahooFantasyTools(YahooFantasyClient fantasyClient)
{
    [McpServerTool(Name = "get_my_leagues"), Description("List Yahoo Fantasy leagues for the authenticated user. Use this first to find league_key values.")]
    public Task<string> GetMyLeaguesAsync(CancellationToken cancellationToken) =>
        fantasyClient.GetMyLeaguesAsync(cancellationToken);

    [McpServerTool(Name = "get_my_teams"), Description("List Yahoo Fantasy teams owned by the authenticated user. Returns team_key values used for roster calls.")]
    public Task<string> GetMyTeamsAsync(CancellationToken cancellationToken) =>
        fantasyClient.GetMyTeamsAsync(cancellationToken);

    [McpServerTool(Name = "get_league"), Description("Get metadata for a Yahoo Fantasy league (name, current week, scoring type, etc.).")]
    public Task<string> GetLeagueAsync(
        [Description("Yahoo league key, e.g. 461.l.12345")] string leagueKey,
        CancellationToken cancellationToken) =>
        fantasyClient.GetLeagueAsync(leagueKey, cancellationToken);

    [McpServerTool(Name = "get_league_standings"), Description("Get standings for a Yahoo Fantasy league.")]
    public Task<string> GetLeagueStandingsAsync(
        [Description("Yahoo league key, e.g. 461.l.12345")] string leagueKey,
        CancellationToken cancellationToken) =>
        fantasyClient.GetLeagueStandingsAsync(leagueKey, cancellationToken);

    [McpServerTool(Name = "get_league_teams"), Description("List all teams in a Yahoo Fantasy league.")]
    public Task<string> GetLeagueTeamsAsync(
        [Description("Yahoo league key, e.g. 461.l.12345")] string leagueKey,
        CancellationToken cancellationToken) =>
        fantasyClient.GetLeagueTeamsAsync(leagueKey, cancellationToken);

    [McpServerTool(Name = "get_team"), Description("Get metadata for a Yahoo Fantasy team.")]
    public Task<string> GetTeamAsync(
        [Description("Yahoo team key, e.g. 461.l.12345.t.1")] string teamKey,
        CancellationToken cancellationToken) =>
        fantasyClient.GetTeamAsync(teamKey, cancellationToken);

    [McpServerTool(Name = "get_team_roster"), Description("Get a team's roster for the current week, or a specific week if provided. Use for your roster or opponents' rosters.")]
    public Task<string> GetTeamRosterAsync(
        [Description("Yahoo team key, e.g. 461.l.12345.t.1")] string teamKey,
        [Description("Optional week number. Omit for current week.")] int? week,
        CancellationToken cancellationToken) =>
        fantasyClient.GetTeamRosterAsync(teamKey, week, cancellationToken);

    [McpServerTool(Name = "get_free_agents"), Description("List available (free agent / waiver) players in a league. Optionally filter by position like QB, WR, RB, TE, K, DEF.")]
    public Task<string> GetFreeAgentsAsync(
        [Description("Yahoo league key, e.g. 461.l.12345")] string leagueKey,
        [Description("Optional position filter, e.g. WR, RB, QB, TE, K, DEF")] string? position,
        [Description("Pagination start index (default 0)")] int start,
        [Description("Page size, max useful around 25 (default 25)")] int count,
        CancellationToken cancellationToken) =>
        fantasyClient.GetFreeAgentsAsync(
            leagueKey,
            position,
            start,
            count <= 0 ? 25 : Math.Min(count, 50),
            cancellationToken);

    [McpServerTool(Name = "get_league_scoreboard"), Description("Get the league scoreboard / matchups for the current week, or a specific week if provided.")]
    public Task<string> GetLeagueScoreboardAsync(
        [Description("Yahoo league key, e.g. 461.l.12345")] string leagueKey,
        [Description("Optional week number. Omit for current week.")] int? week,
        CancellationToken cancellationToken) =>
        fantasyClient.GetLeagueScoreboardAsync(leagueKey, week, cancellationToken);
}
