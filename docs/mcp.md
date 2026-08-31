# MCP

RealityFootball hosts an MCP server over **Streamable HTTP** using the official C# SDK.

| | |
|---|---|
| Package | `ModelContextProtocol.AspNetCore` **2.2.0** |
| Endpoint | `https://localhost:7146/mcp` |
| Tools class | `Mcp/YahooFantasyTools.cs` |

## Tools

| Name | Arguments | Description |
|------|-----------|-------------|
| `get_my_leagues` | — | Leagues for the logged-in Yahoo user. Start here for `league_key`. |
| `get_my_teams` | — | Teams you own. Use for `team_key`. |
| `get_league` | `leagueKey` | League metadata (name, week, etc.). |
| `get_league_standings` | `leagueKey` | Standings. |
| `get_league_teams` | `leagueKey` | All teams in the league. |
| `get_team` | `teamKey` | Team metadata. |
| `get_team_roster` | `teamKey`, optional `week` | Roster for current or specific week. |
| `get_free_agents` | `leagueKey`, optional `position`, `start`, `count` | Available players (`status=A`). |
| `get_league_scoreboard` | `leagueKey`, optional `week` | Matchups / scoreboard. |

Yahoo keys look like:

- League: `461.l.12345`
- Team: `461.l.12345.t.1`

Exact keys come from `get_my_leagues` / `get_my_teams` / `get_league_teams` responses.

## Prerequisites for MCP tools to work

1. API running with `--launch-profile https`
2. Successful `/auth/login` (tokens on disk)
3. Yahoo Fantasy API access **approved** for your Client ID

Without (3), tools will error even if OAuth login succeeded.

## Cursor configuration

With the API already running, add an MCP server entry that points at the HTTP endpoint.

Example (Cursor MCP settings — exact UI/file may vary by Cursor version):

```json
{
  "mcpServers": {
    "realityfootball": {
      "url": "https://localhost:7146/mcp"
    }
  }
}
```

Notes:

- Use **HTTPS** and port **7146** to match the launch profile.
- The API process must be running before Cursor connects.
- Local HTTPS may require the trusted `dotnet` dev certificate (`dotnet dev-certs https --trust`).

### Typical advice workflow

Ask the model questions like:

- “What leagues am I in?”
- “Show my roster and this week’s matchup.”
- “Who are the top available WRs in my league?”

The model should call tools in roughly this order:

```
get_my_leagues → get_my_teams → get_team_roster
                              → get_league_scoreboard
                              → get_free_agents
```

## ChatGPT (later)

ChatGPT needs a reachable HTTPS MCP server (deployed) or OpenAI’s Secure MCP Tunnel for a local process. Same tools; different hosting. See README “Current limitations.”

## Adding a new tool

1. Add a method on `YahooFantasyClient` that calls the Yahoo path.
2. Add a matching `[McpServerTool]` method on `YahooFantasyTools`.
3. Optionally add a REST route in `Program.cs` for manual testing.
4. Rebuild and restart the API.
