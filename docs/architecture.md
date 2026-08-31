# Architecture

## Goal

Provide live Yahoo Fantasy league data to AI clients through MCP tools, backed by a small ASP.NET Core API that owns OAuth and Yahoo HTTP calls.

## High-level diagram

```
┌─────────────────────┐
│  Cursor / ChatGPT   │
│  (MCP client)       │
└──────────┬──────────┘
           │ HTTP MCP (/mcp)
           ▼
┌──────────────────────────────────────────┐
│           RealityFootball.Api            │
│                                          │
│  Program.cs                              │
│    ├─ /auth/*     (OAuth browser flow)   │
│    ├─ /yahoo/*    (REST for debugging)   │
│    └─ /mcp        (MCP tool host)        │
│                                          │
│  YahooFantasyTools  ──► YahooFantasyClient
│  (MCP attributes)         │
│                           ▼
│                    YahooOAuthService
│                           │
│                    YahooTokenStore
│                    (data/yahoo-tokens.json)
└───────────────────────────┬──────────────┘
                            │ HTTPS + Bearer token
                            ▼
                 Yahoo Fantasy Sports API
                 fantasysports.yahooapis.com/fantasy/v2
```

## Design principles

1. **Yahoo logic once** — REST and MCP both call `YahooFantasyClient`. No duplicated HTTP.
2. **OAuth before tools** — MCP tools fail clearly if tokens are missing; login is a separate browser step.
3. **Minimal APIs** — routes live in `Program.cs` for a small surface area; services hold the real logic.
4. **Secrets out of git** — Client ID/Secret via user-secrets; tokens in a gitignored file.

## Components

### `YahooOAuthService`

- Builds the Yahoo authorize URL (`/oauth2/request_auth`).
- Exchanges authorization code for access + refresh tokens (`/oauth2/get_token`).
- Refreshes expired access tokens before Fantasy calls.
- Uses HTTP Basic auth with Client ID + Secret for token endpoints.

Fantasy Sports permissions are bound on the **Yahoo Developer app** (and require Yahoo’s access approval). The authorize URL does not send a custom `scope` query parameter.

### `YahooTokenStore`

- Persists tokens to `data/yahoo-tokens.json` under the API content root.
- Thread-safe file read/write.
- Cleared by `/auth/logout`.

### `YahooFantasyClient`

Thin authenticated HTTP client for Fantasy `v2` resources:

| Method | Yahoo path (relative) |
|--------|------------------------|
| Games | `users;use_login=1/games` |
| My leagues | `users;use_login=1/games/leagues` |
| My teams | `users;use_login=1/games/teams` |
| League | `league/{league_key}` |
| Standings | `league/{league_key}/standings` |
| League teams | `league/{league_key}/teams` |
| Team | `team/{team_key}` |
| Roster | `team/{team_key}/roster[;week=N]` |
| Free agents | `league/{league_key}/players;status=A;...` |
| Scoreboard | `league/{league_key}/scoreboard[;week=N]` |

Responses are returned as Yahoo’s JSON (not yet mapped to strongly typed DTOs).

### `YahooFantasyTools`

MCP tool type (`[McpServerToolType]`) that wraps the client methods with names/descriptions for the AI.

Registered in DI via:

```csharp
builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithTools<YahooFantasyTools>();

app.MapMcp("/mcp");
```

## Request flows

### A. First-time login

```
Browser GET /auth/login
  → set state cookie
  → redirect to Yahoo
Yahoo redirects GET /auth/callback?code=...&state=...
  → validate state
  → exchange code for tokens
  → save tokens
  → return success JSON
```

### B. Data call (REST or MCP)

```
Client → /yahoo/... or MCP tool
  → YahooFantasyClient.Get*
  → YahooOAuthService.GetValidAccessTokenAsync
       (refresh if near expiry)
  → GET fantasysports.yahooapis.com/... Bearer {token}
  → return JSON to client / AI
```

## Runtime profiles

| Profile | URLs | Use |
|---------|------|-----|
| `https` | `https://localhost:7146`, `http://localhost:5277` | OAuth + daily work |
| `http` | `http://localhost:5277` | Quick non-OAuth checks only |

Yahoo redirect URI in this project: `https://localhost:7146/auth/callback`.

## Planned evolution

| Area | Direction |
|------|-----------|
| Typing | Parse Yahoo JSON into models instead of raw strings |
| Transport | Optional stdio MCP for Cursor-only local launch |
| ChatGPT | Deploy HTTPS MCP or OpenAI Secure MCP Tunnel |
| Auth | Protect public MCP endpoint if hosted remotely |
| Controllers | Optional refactor if route surface grows large |

## Solution layout

```
realityfootball/
├── RealityFootball.sln / .slnx
├── nuget.config                 # Prefer nuget.org for this repo
├── README.md
├── docs/
└── src/
    └── RealityFootball.Api/
```
