# Setup

## 1. Install .NET 10

```powershell
dotnet --version
```

Expect `10.x`. Install from [dotnet.microsoft.com/download](https://dotnet.microsoft.com/download) if needed.

## 2. Yahoo Developer app

1. Create or open an app at [developer.yahoo.com/apps](https://developer.yahoo.com/apps/).
2. Application type: **Web Application**.
3. Redirect URI (must match exactly):

   ```
   https://localhost:7146/auth/callback
   ```

4. Save **Client ID** (Consumer Key) and **Client Secret** (Consumer Secret).

### Fantasy Sports API approval (required)

As of mid-2026, Yahoo gates Fantasy Sports API access behind an approval process. Creating an app alone is not enough.

1. Go to [sports.yahoo.com/developer/access](https://sports.yahoo.com/developer/access/).
2. Submit an access request.
3. Enter your existing **Client ID** so Yahoo can bind approval to your app.
4. **Do not delete and recreate** the app while waiting — that destroys the ID under review.

Until approved:

- `/auth/login` may succeed and save tokens.
- `/yahoo/*` and MCP Fantasy tools may return `additional_authorization_required` or similar.

## 3. Trust the HTTPS certificate

Yahoo requires HTTPS redirect URIs for this setup.

```powershell
dotnet dev-certs https --trust
```

## 4. Clone / open the repo

```powershell
cd c:\Users\samga\OneDrive\Desktop\realityfootball
dotnet restore
dotnet build
```

This repo includes a local `nuget.config` that clears other feeds and uses nuget.org only (avoids private Azure DevOps feed 401s from machine-wide NuGet config).

## 5. Configure user secrets

From the API project:

```powershell
cd src\RealityFootball.Api

dotnet user-secrets init
dotnet user-secrets set "Yahoo:ClientId" "YOUR_CLIENT_ID"
dotnet user-secrets set "Yahoo:ClientSecret" "YOUR_CLIENT_SECRET"
dotnet user-secrets set "Yahoo:RedirectUri" "https://localhost:7146/auth/callback"

dotnet user-secrets list
```

Never put the Client Secret in `appsettings.json` or commit it.

`appsettings.json` only holds an empty placeholder section:

```json
"Yahoo": {
  "ClientId": "",
  "ClientSecret": "",
  "RedirectUri": "https://localhost:7146/auth/callback"
}
```

User-secrets override these in Development.

## 6. Run the API

From the repo root:

```powershell
dotnet run --project src/RealityFootball.Api --launch-profile https
```

Or with hot reload while coding:

```powershell
dotnet watch run --project src/RealityFootball.Api --launch-profile https
```

You should see something like:

```text
Now listening on: https://localhost:7146
```

## 7. Complete OAuth once

1. Open `https://localhost:7146/auth/login`
2. Sign in to Yahoo and approve the app
3. You should land on `/auth/callback` with a success message
4. Confirm: `https://localhost:7146/auth/status`

Tokens are written to:

```text
src/RealityFootball.Api/data/yahoo-tokens.json
```

That path is gitignored.

## 8. Smoke-test Fantasy calls

After Yahoo approves Fantasy access:

```text
GET https://localhost:7146/yahoo/leagues
GET https://localhost:7146/yahoo/teams
```

Then configure MCP (see [mcp.md](mcp.md)).

## Checklist

```
[ ] .NET 10 SDK installed
[ ] Yahoo app created (keep Client ID)
[ ] Fantasy API access requested / approved
[ ] Redirect URI = https://localhost:7146/auth/callback
[ ] user-secrets set (ClientId, ClientSecret, RedirectUri)
[ ] HTTPS cert trusted
[ ] App runs on https://localhost:7146
[ ] /auth/login succeeds
[ ] /yahoo/leagues returns JSON (after approval)
```
