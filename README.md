# Bikeapelago Monorepo

Bikeapelago is a location-based Archipelago integration with:
- a `.NET 10` API (`api/`)
- a React + Vite frontend workspace (`frontend/`)

## Repository Layout

- `api/` - ASP.NET Core API, EF Core/PostgreSQL, SignalR hub
- `api.Tests/` - xUnit API test project
- `frontend/` - pnpm workspace
- `frontend/packages/apps/bikepelago-app/` - main game app
- `frontend/packages/apps/admin-ui/` - admin dashboard

## Prerequisites

- Docker + Docker Compose
- .NET SDK 10+
- Node.js 20+ (Node 25 currently used in frontend Docker image)
- pnpm 10+

## Quick Start

1. Copy environment config:
```bash
cp .env.example .env
```
2. Fill required values in `.env`:
- `DB_PASSWORD`
- `OSM_DISCOVERY_PASSWORD`
- `MAPBOX_API_KEY`
- `JWT_KEY`
- `ADMIN_PASSWORD`

3. Start the stack:
```bash
docker compose up
```

Current `docker-compose.yml` behavior:
- Starts API + frontend dev servers + both PostGIS databases by default
- `archipelago` service is optional via `--profile archipelago`

Key URLs:
- API: `http://localhost:5054`
- Game frontend (Vite): `http://localhost:18182`
- Admin frontend (Vite): `http://localhost:18183`

## Manual Development

### API

```bash
cd api
dotnet restore
dotnet run
```

API local URL (launch settings): `http://localhost:5054`

### Frontend

```bash
cd frontend
pnpm install
pnpm --filter "@bikeapelago/bikepelago-app" run dev
```

Admin UI:
```bash
pnpm --filter "@bikeapelago/admin-ui" run dev
```

Typical local URLs:
- Game app: `http://localhost:5173`
- Admin UI: `http://localhost:5174` (or next free Vite port)

## Testing and Validation

### API

```bash
# from repo root
dotnet test api.Tests/Bikeapelago.Api.Tests.csproj

# or from api/
dotnet build
```

### Frontend

```bash
cd frontend
pnpm -r --if-present run lint
pnpm --filter "@bikeapelago/bikepelago-app" run test:run
pnpm --filter "@bikeapelago/admin-ui" run tsc
```

Note: there is no repository-level Playwright `test:e2e` script currently.

## Configuration

### Root `.env`

Used by Docker Compose and API startup.

Important variables:
- `DB_USER`, `DB_PASSWORD`, `DB_BIKEAPELAGO_PORT`
- `OSM_DISCOVERY_HOST`, `OSM_DISCOVERY_PORT`, `OSM_DISCOVERY_USER`, `OSM_DISCOVERY_PASSWORD`
- `MAPBOX_API_KEY`
- `JWT_KEY`
- `ADMIN_EMAIL`, `ADMIN_PASSWORD`
- Optional for production frontend proxying: `API_PROXY_URL`, `API_HUBS_URL`

### Frontend app `.env`

Each app can define Vite API target:
- `frontend/packages/apps/bikepelago-app/.env`
- `frontend/packages/apps/admin-ui/.env`

Variable:
- `VITE_PUBLIC_API_URL` — optional for browser dev (Vite proxy handles it), **required** for native iOS/Android builds (must be a full URL, e.g. `https://bikeapelago.alexkibler.com`)

### CORS (native app support)

The API CORS policy is driven by `AllowedOrigins` in `docker-compose.deploy.yml`. Native Capacitor apps send requests from the `capacitor://localhost` origin, which must be explicitly allowed:

```yaml
AllowedOrigins__2: ${ALLOWED_ORIGIN_2:-capacitor://localhost}
```

This is already set in `docker-compose.deploy.yml`. When adding new allowed origins, extend the numbered sequence (`AllowedOrigins__3`, etc.).

## Additional Docs

- [Architecture](ARCHITECTURE.md)
- [API README](api/README.md)
- [Frontend README](frontend/README.md)
- [Claude Instructions](CLAUDE.md)
- [Gemini Instructions](GEMINI.md)
- [Codex Instructions](CODEX.md)
