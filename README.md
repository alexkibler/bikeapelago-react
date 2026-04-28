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

## Run Modes

### Production Full Stack

Runs the GHCR-published API, game frontend, and admin frontend against the production app database and shared OSM database.

```bash
docker compose --env-file .env.prod -f docker-compose.deploy.yml pull
docker compose --env-file .env.prod -f docker-compose.deploy.yml up -d --force-recreate
```

Key URLs/ports:
- API: `http://localhost:5054`
- Game frontend: `http://localhost:8192`
- Admin frontend: `http://localhost:8183`

### Dev Full Stack

Runs a containerized dev API plus GHCR-published runtime frontends against `postgis-bikeapelago-new` and the shared OSM database.

```bash
docker compose --env-file .env.dev -f docker-compose.dev-db.yml up -d --build
```

Key URLs/ports:
- API: `http://localhost:5055`
- Game frontend: `http://localhost:8193`
- Admin frontend: `http://localhost:8194`
- Dev app DB: `localhost:5435`
- Shared OSM DB: `localhost:5433`

### Local Servers Against Dev DB

Use this when you want `dotnet run` and local pnpm/Vite servers, while reusing the dev Docker databases.

```bash
docker compose --env-file .env.dev -f docker-compose.dev-db.yml up -d postgis-bikeapelago-new postgis-osm

cd api
dotnet run
```

In another shell:

```bash
cd frontend
pnpm install
pnpm run dev:local:app
```

In another shell:

```bash
cd frontend
pnpm run dev:local:admin
```

Local URLs:
- API: `http://localhost:5056`
- Game app: `http://localhost:5175`
- Admin UI: `http://localhost:5176`

### Joe/Generic Local Compose

The base `docker-compose.yml` remains the generic local setup for another developer running their own isolated stack.

```bash
cp .env.example .env
docker compose up
```

## Environment Files

Environment files are intentionally scoped by run mode:
- `.env.prod` - production full stack secrets and connection strings.
- `.env.dev` - containerized dev full stack secrets and dev DB wiring.
- `.env.local` - `dotnet run` wiring for local API against the dev DB.
- `.env.example` - safe template with no secrets.

Docker Compose cannot reliably self-select one of these files from inside the compose YAML because variable interpolation happens before service `env_file` values are applied. Always pass the intended file with `--env-file`.

Frontend app `.env.local` files are app-local because Vite loads env files from each app directory.

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

### Build

```bash
cd frontend
pnpm run build
```

For API build-only validation:

```bash
dotnet build api/Bikeapelago.Api.csproj
```

### Native iOS

Native builds must set `VITE_PUBLIC_API_URL` to the full HTTPS backend URL because Capacitor WebViews cannot use the Vite dev proxy.

```bash
cd frontend/packages/apps/bikepelago-app
pnpm run build
npx cap sync ios
```

## Configuration

### Root Environment

Used by Docker Compose and API startup, depending on run mode.

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
