# RealityFootball

ASP.NET Core Web API that connects to the **Yahoo Fantasy Sports API** and exposes league data as an **MCP (Model Context Protocol)** server — so AI clients (Cursor now, ChatGPT later) can answer questions about your league, roster, free agents, and matchups.

| | |
|---|---|
| **Status** | Early development — OAuth + Fantasy client + MCP tools in place |
| **Runtime** | .NET 10 (`net10.0`) |
| **MCP SDK** | `ModelContextProtocol.AspNetCore` **2.2.0** |
| **Yahoo Fantasy API** | `fantasy/v2` (read-only; requires Yahoo app approval) |

---

## What it does

```
You (in Cursor / ChatGPT)
        ↓
   MCP tools (/mcp)
        ↓
 YahooFantasyClient
        ↓
 Yahoo Fantasy Sports API
```

1. You log in once via Yahoo OAuth (`/auth/login`).
2. Tokens are stored locally (not in git).
3. REST endpoints and MCP tools call Yahoo on demand.
4. The AI reasons over that live data to give league advice.

This app does **not** invent advice itself — it supplies accurate league data as tools.

---

## Architecture (short)

```
src/RealityFootball.Api/
├── Program.cs                 # Minimal API routes + MCP host
├── Options/YahooOptions.cs    # ClientId / Secret / RedirectUri
├── Models/YahooTokens.cs      # Access + refresh token shape
├── Services/
│   ├── YahooOAuthService.cs   # Login URL, code exchange, refresh
│   ├── YahooTokenStore.cs     # Local token file (gitignored)
│   └── YahooFantasyClient.cs  # Fantasy API HTTP calls
└── Mcp/YahooFantasyTools.cs   # MCP tool wrappers
```

**Layers:**

| Layer | Responsibility |
|-------|----------------|
| HTTP routes (`/auth/*`, `/yahoo/*`) | Browser / curl testing |
| MCP (`/mcp`) | AI tool surface |
| Services | OAuth + Yahoo API |
| User secrets + token file | Credentials (never commit) |

More detail: [docs/architecture.md](docs/architecture.md)

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (`dotnet --version` → 10.x)
- Yahoo Developer app + **Fantasy Sports API access approval**  
  Apply at: [sports.yahoo.com/developer/access](https://sports.yahoo.com/developer/access/)  
  Use your existing **Client ID** — do not delete and recreate the app.
- Trusted HTTPS dev certificate (for Yahoo redirect):  
  `dotnet dev-certs https --trust`

---

## Quick start

```powershell
cd c:\Users\samga\OneDrive\Desktop\realityfootball

# Restore / build
dotnet restore
dotnet build

# Configure Yahoo secrets (once)
cd src\RealityFootball.Api
dotnet user-secrets init   # if not already done
dotnet user-secrets set "Yahoo:ClientId" "YOUR_CLIENT_ID"
dotnet user-secrets set "Yahoo:ClientSecret" "YOUR_CLIENT_SECRET"
dotnet user-secrets set "Yahoo:RedirectUri" "https://localhost:7146/auth/callback"

# Run with HTTPS (required for Yahoo OAuth redirect)
cd ..\..
dotnet run --project src/RealityFootball.Api --launch-profile https
```

Then in a browser:

1. `https://localhost:7146/auth/login` → approve on Yahoo  
2. `https://localhost:7146/auth/status` → should show `authenticated: true`  
3. `https://localhost:7146/yahoo/leagues` → live league JSON (needs Yahoo Fantasy approval)

Full setup: [docs/setup.md](docs/setup.md)

---

## MCP tools

| Tool | Purpose |
|------|---------|
| `get_my_leagues` | List your leagues → get `league_key` |
| `get_my_teams` | List your teams → get `team_key` |
| `get_league` | League metadata |
| `get_league_standings` | Standings |
| `get_league_teams` | All teams in a league |
| `get_team` | Team metadata |
| `get_team_roster` | Roster (yours or anyone’s) |
| `get_free_agents` | Available players (optional position) |
| `get_league_scoreboard` | Matchups / scoreboard |

MCP HTTP endpoint: **`https://localhost:7146/mcp`**

Cursor / client wiring: [docs/mcp.md](docs/mcp.md)

---

## REST endpoints (for testing)

| Method | Path | Notes |
|--------|------|-------|
| GET | `/` | Index of routes |
| GET | `/auth/login` | Start Yahoo OAuth |
| GET | `/auth/callback` | OAuth redirect target |
| GET | `/auth/status` | Token present / expired? |
| GET | `/auth/logout` | Clear local tokens |
| GET | `/yahoo/games` | Games for logged-in user |
| GET | `/yahoo/leagues` | Your leagues |
| GET | `/yahoo/teams` | Your teams |
| GET | `/yahoo/leagues/{leagueKey}` | League metadata |
| GET | `/yahoo/leagues/{leagueKey}/standings` | Standings |
| GET | `/yahoo/leagues/{leagueKey}/teams` | League teams |
| GET | `/yahoo/leagues/{leagueKey}/free-agents` | `?position=WR&start=0&count=25` |
| GET | `/yahoo/leagues/{leagueKey}/scoreboard` | `?week=1` optional |
| GET | `/yahoo/teams/{teamKey}` | Team metadata |
| GET | `/yahoo/teams/{teamKey}/roster` | `?week=1` optional |
| — | `/mcp` | MCP Streamable HTTP |

---

## Configuration

Secrets live in **.NET user-secrets** (machine-local), not in the repo.

| Key | Example |
|-----|---------|
| `Yahoo:ClientId` | From Yahoo Developer Network |
| `Yahoo:ClientSecret` | From Yahoo Developer Network |
| `Yahoo:RedirectUri` | `https://localhost:7146/auth/callback` |

Redirect URI must match the Yahoo app setting **exactly** (HTTPS + port `7146`).

Tokens after login: `src/RealityFootball.Api/data/yahoo-tokens.json` (gitignored).

---

## Versions

| Component | Version |
|-----------|---------|
| Target framework | `net10.0` |
| ASP.NET Core OpenAPI | 10.0.10 |
| Microsoft.OpenApi | 2.9.0 |
| ModelContextProtocol.AspNetCore | 2.2.0 |
| Yahoo Fantasy API | v2 |

---

## Docs

| Doc | Contents |
|-----|----------|
| [docs/architecture.md](docs/architecture.md) | System design, request flow, folder layout |
| [docs/setup.md](docs/setup.md) | Yahoo app, secrets, HTTPS, first login |
| [docs/mcp.md](docs/mcp.md) | MCP tools + Cursor configuration |
| [docs/troubleshooting.md](docs/troubleshooting.md) | Common errors (including API approval) |

---

## Current limitations

- Yahoo Fantasy access requires **manual approval**; OAuth can succeed while `/yahoo/*` returns `additional_authorization_required` until approved.
- Read-only Fantasy access (Yahoo’s current default).
- Tokens stored as a local file (fine for personal use; not multi-user).
- ChatGPT remote hosting / Secure MCP Tunnel not set up yet.

---

## License

Private / personal use unless otherwise stated.
