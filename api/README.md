# Bikeapelago API (`api/`)

ASP.NET Core `.NET 10` API for authentication, sessions, node generation, routing, and Archipelago sync.

## Tech Stack

- ASP.NET Core Web API
- EF Core + PostgreSQL/PostGIS
- SignalR (`/hubs/archipelago`)
- Mapbox APIs (routing + node validation)
- YARP reverse proxy for PocketBase passthrough

## Local Run

From `api/`:

```bash
dotnet restore
dotnet build
dotnet run
```

Default development URL: `http://localhost:5054` (see `Properties/launchSettings.json`).

Swagger UI is enabled in Development.

## Configuration

The app loads `../.env` at startup and also reads `appsettings*.json`.

Required/important environment values:
- `ConnectionStrings__PostGis` (or `ConnectionStrings:PostGis`)
- `ConnectionStrings__OsmDiscovery`
- `MAPBOX_API_KEY`
- `JWT_KEY` (must be at least 32 chars)
- `ADMIN_EMAIL`, `ADMIN_PASSWORD`

CORS origins are defined in `appsettings.Development.json` under `AllowedOrigins`.

## Database and Migrations

- Migrations live in `api/Migrations/`.
- On startup, API attempts `Database.MigrateAsync()`.

Common commands:

```bash
# from api/
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

## Test Commands

From repository root:

```bash
dotnet test api.Tests/Bikeapelago.Api.Tests.csproj
```

## Key Endpoints

- `POST /api/auth/register`
- `POST /api/auth/login`
- `GET /api/sessions`
- `POST /api/sessions`
- `POST /api/sessions/{id}/generate`
- `POST /api/sessions/setup-from-route`
- `PATCH /api/sessions/{id}`
- `GET /api/sessions/{id}/nodes`
- `PATCH /api/nodes/{id}`
- `POST /api/discovery/validate-nodes`
- `POST /api/sessions/{id}/route-to-available`
- `GET /hubs/archipelago` (SignalR hub)

See also: [API_ENDPOINTS.md](API_ENDPOINTS.md)

## OSM Discovery Notes

Primary production path uses PostGIS-backed node discovery (`PostGisOsmDiscoveryService`).
Service selection priority:
1. `USE_MOCK_OVERPASS=true`
2. `ConnectionStrings:OsmDiscovery` configured
3. `OsmDiscovery:PbfPath` configured
4. fallback external Overpass service

## Related Docs

- [Root README](../README.md)
- [API Claude instructions](CLAUDE.md)
- [API Gemini instructions](GEMINI.md)
- [API Codex instructions](CODEX.md)
