# Troubleshooting

## `additional_authorization_required` on `/yahoo/*`

**Symptom:** `/auth/login` works and `/auth/status` shows authenticated, but Fantasy endpoints fail with:

```text
OAuth oauth_problem="additional_authorization_required"
```

**Cause:** Your Yahoo Developer app is not approved for Fantasy Sports API access. Login tokens alone are not enough.

**Fix:**

1. Request access at [sports.yahoo.com/developer/access](https://sports.yahoo.com/developer/access/).
2. Include your existing **Client ID**.
3. Wait for Yahoo approval.
4. After approval, run `/auth/login` again (fresh token), then retry `/yahoo/leagues`.

**Do not** delete and recreate the Yahoo app while waiting — that orphans the approval request.

---

## `invalid_scope` on login

**Cause:** Sending an unsupported `scope` query parameter on Yahoo’s authorize URL.

**Status:** This project’s authorize URL does **not** send `scope`. If you reintroduce it, remove it and rely on Yahoo app permissions + Fantasy approval.

---

## Redirect URI mismatch

**Symptom:** Yahoo rejects login or callback fails.

**Fix:** Redirect URI must be identical in all three places:

1. Yahoo Developer app settings  
2. User secret `Yahoo:RedirectUri`  
3. Running app URL / port  

Expected for local HTTPS profile:

```text
https://localhost:7146/auth/callback
```

---

## `Failed to determine the https port for redirect`

**Cause:** Running the `http` profile while `UseHttpsRedirection()` is enabled.

**Fix:** Use the HTTPS profile:

```powershell
dotnet run --project src/RealityFootball.Api --launch-profile https
```

---

## NuGet `401 Unauthorized` for Azure DevOps feed

**Cause:** Machine-wide NuGet.config includes a private feed (e.g. Pinwheel `EqsFeed`) without credentials.

**Fix:** This repo’s `nuget.config` clears extra feeds and uses nuget.org. Keep that file at the repo root.

---

## `Unable to find a project to restore`

**Cause:** Running `dotnet restore` at the root before the API project was added to the solution.

**Fix:**

```powershell
dotnet sln add src/RealityFootball.Api/RealityFootball.Api.csproj
dotnet restore
```

Or restore the project file directly.

---

## MCP tools return “Not authenticated”

**Cause:** No local token file, or tokens cleared.

**Fix:**

1. Ensure the API is running.
2. Visit `https://localhost:7146/auth/login`.
3. Confirm `https://localhost:7146/auth/status`.

---

## Browser certificate warnings on `https://localhost:7146`

```powershell
dotnet dev-certs https --trust
```

Then restart the browser / API.

---

## After Yahoo approval still failing

1. Confirm `/auth/status` → authenticated.  
2. Logout and login again: `/auth/logout` then `/auth/login`.  
3. Retry `/yahoo/games` (simplest Fantasy call).  
4. Verify the Client ID in user-secrets is the same ID submitted for approval.
