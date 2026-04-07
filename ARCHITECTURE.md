# Bikeapelago Architecture

This document describes the architectural decisions and design patterns for the React/.NET implementation.

## High-Level Diagram

```
React Frontend (Vite)
        │
        │ REST (via Vite proxy → localhost:5054)
        ▼
.NET 10 Web API (ASP.NET Core)
        │
        ├── EF Core → PostgreSQL/PostGIS (bikeapelago DB)
        │     └── GameSessions, MapNodes, Users, Routes, Activities
        │
        ├── PostGisOsmDiscoveryService → PostgreSQL/PostGIS (osm_discovery DB)
        │     └── planet_osm_nodes (flex-imported, 4326, GiST indexed)
        │
        ├── YARP Reverse Proxy
        │     ├── /api/pb/** → PocketBase (legacy, phasing out)
        │     └── /api/gh/** → GraphHopper
        │
        └── GraphHopper (routing validation)
```

## Frontend: React + Zustand

SPA built with Vite and React. State management via Zustand:
- `userStore`: Auth state and profile
- `gameStore`: Session data, Archipelago state, location tracking
- `mapStore`: Leaflet map state, routing params, active waypoints

**Styling**: Tailwind CSS + DaisyUI

## Backend: .NET 10 Web API

ASP.NET Core 10, repository pattern, dependency injection:
- **Controllers**: Thin HTTP endpoints (Sessions, Nodes, Auth)
- **Services**: Business logic — node generation, OSM discovery, routing
- **Repositories**: EF Core (prod) + Mock (tests/E2E) implementations

### Authentication
JWT-based. Stateless. `IUserRepository` handles registration/login.

### Node Generation (`/api/sessions/{id}/generate`)

The generate flow:
1. Fetch random nodes from `osm_discovery` PostGIS DB via `PostGisOsmDiscoveryService`
2. Delete existing nodes for the session
3. Bulk insert new `MapNode` records (single `SaveChangesAsync` — NOT per-row)
4. Update session status to Active

**Critical**: Use `CreateRangeAsync` not `CreateAsync` in a loop. Per-row saves caused 5000 individual transactions and consistent timeouts even for 25 nodes.

### OSM Discovery Service Selection

`OsmDiscoveryService` selects implementation at startup:
1. `USE_MOCK_OVERPASS=true` → MockOsmDiscoveryService (for E2E tests)
2. `ConnectionStrings:OsmDiscovery` set → **PostGisOsmDiscoveryService** (preferred)
3. `OsmDiscovery:PbfPath` set → PbfOsmDiscoveryService
4. Fallback → OverpassOsmDiscoveryService (**avoid** — slow external HTTP, causes 504s)

Always set `ConnectionStrings:OsmDiscovery` in `appsettings.Development.json` to avoid falling back to Overpass.

## OSM Data Infrastructure

### Database: `osm_discovery` (PostgreSQL + PostGIS)

The game queries `planet_osm_nodes` — imported via osm2pgsql flex output:
- **Table**: `planet_osm_nodes` — columns: `id`, `geom geometry(Point,4326)`
- **Index**: GiST spatial index on `geom` (created by osm2pgsql after import)
- **Projection**: 4326 (WGS84) — no coordinate transforms needed at query time

### Why flex output over pgsql output

The old pgsql output (`planet_osm_point`) stores geometry in 3857 (Web Mercator) and creates 60+ tag columns we don't need. The flex output creates a lean table in 4326 with just a geometry column. This means:
- No `ST_Transform` in queries → GiST index is used directly
- Smaller rows → faster scans and smaller index
- pgsql output is officially deprecated by osm2pgsql

### Query Pattern

```sql
SELECT ST_X(geom)::float8, ST_Y(geom)::float8
FROM planet_osm_nodes
WHERE ST_DWithin(
    geom::geography,
    ST_SetSRID(ST_MakePoint(@lon, @lat), 4326)::geography,
    @radius
)
ORDER BY RANDOM()
LIMIT @count
```

Filtering on native `geom` (4326) lets PostGIS use the GiST index. The `::geography` cast makes the radius accurate in meters.

### OSM Data File

Coverage: **Contiguous US + Ontario**

Single merged PBF: `graphhopper/data/us-states-merged.osm.pbf`
- Downloaded from Geofabrik: `us-latest.osm.pbf` + `ontario-latest.osm.pbf`
- Merged with osmium: `osmium merge us-latest.osm.pbf ontario-latest.osm.pbf -o us-states-merged.osm.pbf`
- Used by both osm2pgsql (node discovery) and GraphHopper (routing)

See `import-states.sh` in the repo root for the import procedure.

### Updating OSM Data (Zero Downtime)

`import-states.sh` uses a blue/green table swap:
1. Import into `planet_osm_nodes_new` (staging) via `osm2pgsql-flex-staging.lua`
2. Atomically rename: `planet_osm_nodes` → `planet_osm_nodes_old`, `planet_osm_nodes_new` → `planet_osm_nodes`
3. Drop `planet_osm_nodes_old`

Active game sessions are unaffected — their nodes are stored in `MapNodes`. Only new `/generate` calls hit `planet_osm_nodes`, and they stay live throughout the swap.

### Import Gotchas

- **Must use `--create` (not `--append`) without `--slim`**: The flex lua script only defines the output table, not slim node cache tables. `--append` requires `--slim`.
- **Use the arm64 image**: `osm2pgsql-arm64:latest` (built locally). `iboates/osm2pgsql` is amd64-only and runs under Rosetta emulation — slower and causes platform warnings.
- **Don't use `iboates/osm2pgsql` with `--slim --append`**: Causes "extra data after last expected column" errors due to version mismatch with the lua script.
- **Kill zombie DB connections before importing**: A stuck importer can leave backend connections holding locks on `planet_osm_ways`, blocking `DROP TABLE`. Use `pg_terminate_backend()` to clear them.
- **pgsql output is incompatible with flex append**: Don't mix output modes across imports into the same database.

## GraphHopper

Routing engine for node validation and navigation.

- Runs as Docker container, reads from the merged PBF file
- Configured via `graphhopper/config.yml`
- Takes a single PBF file via `PBF_FILE` env var — does not support multiple files natively
- Proxied through the .NET API at `/api/gh/**`

## Deployment

All services run via `docker-compose.yml` in the repo root:

| Service | Port | Notes |
|---|---|---|
| `bikeapelago-api` | 8080 | .NET API |
| `bikeapelago-react` | 8182 | Nginx + React SPA |
| `postgis` | 5432 | Shared PostGIS instance (two DBs: `bikeapelago`, `osm_discovery`) |
| `graphhopper` | 8989 | Routing engine |
| `osm-importer` | — | Runs once and exits |

In development, the Vite dev server proxies `/api` to `localhost:5054`.
