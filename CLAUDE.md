# Bikeapelago Claude Instructions

Global guidance for Claude working in this monorepo.

## Critical Safety Rules

- Never run direct SQL mutations (`psql`, `docker exec ... psql`, ad-hoc DDL/DML).
- Use EF Core migrations for schema updates.
- If a command will modify database contents directly, stop and ask first.
- For frontend tool/API calls, prefer RTK Query wherever the app is already wired for RTK. Use existing shared request helpers or direct `fetch` only when RTK is not available, the call is intentionally one-off, or the surrounding code already has a narrower local pattern.

## Repository Layout

- `api/` - .NET 10 API
- `api.Tests/` - API unit/integration tests
- `frontend/` - pnpm workspace with game app + admin UI

## Standard Commands

### API (`api/`)

```bash
dotnet build
dotnet run
dotnet format
```

Tests:
```bash
dotnet test ../api.Tests/Bikeapelago.Api.Tests.csproj
```

### Frontend (`frontend/`)

```bash
pnpm install
pnpm -r --if-present run build
pnpm -r --if-present run lint
pnpm --filter "@bikeapelago/bikepelago-app" run test:run
pnpm --filter "@bikeapelago/admin-ui" run tsc
```

Dev servers:
```bash
pnpm --filter "@bikeapelago/bikepelago-app" run dev
pnpm --filter "@bikeapelago/admin-ui" run dev
```

## Configuration Notes

- Root `.env` drives Docker + API configuration.
- Frontend app `.env` files set `VITE_PUBLIC_API_URL`.
- API local URL is `http://localhost:5054`.

## Documentation Rule

If behavior, scripts, or environment variables change, update:
- `README.md`
- `api/README.md`
- `frontend/README.md`
- relevant app-level docs under `frontend/packages/apps/*/`
