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
        │     ├── planet_osm_nodes (flex-imported, 4326)
        │     ├── planet_osm_ways (cycling/walking-safe ways, 4326, GiST indexed)
        │     └── planet_osm_way_nodes (way↔node relationships)
        │
        └── MapboxRoutingService (routing via Mapbox APIs)
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

Three tables work together to ensure generated nodes are on actual roads:

- **`planet_osm_nodes`**: All OSM nodes in the dataset
  - Columns: `id` (int8), `geom` (Point, 4326)
  - Index: GiST on `geom` (for spatial queries)

- **`planet_osm_ways`**: Roads/paths filtered to cycling/walking-safe only
  - Columns: `id` (int8), `geom` (LineString, 4326), `cycling_safe` (bool), `walking_safe` (bool)
  - Index: GiST on `geom` (primary filter)
  - Excludes: motorways, ways tagged `bicycle=no`, `foot=no`

- **`planet_osm_way_nodes`**: Mapping of which nodes belong to which ways
  - Columns: `way_id` (int8), `node_id` (int8)
  - Indexes: On both columns for fast joins

**Projection**: All use 4326 (WGS84) — no coordinate transforms needed at query time

### Why flex output over pgsql output

The old pgsql output (`planet_osm_point`) stores geometry in 3857 (Web Mercator) and creates 60+ tag columns we don't need. The flex output creates a lean table in 4326 with just a geometry column. This means:
- No `ST_Transform` in queries → GiST index is used directly
- Smaller rows → faster scans and smaller index
- pgsql output is officially deprecated by osm2pgsql

### Query Pattern

```sql
SELECT DISTINCT ST_X(n.geom)::float8, ST_Y(n.geom)::float8
FROM planet_osm_ways w
INNER JOIN planet_osm_way_nodes wn ON w.id = wn.way_id
INNER JOIN planet_osm_nodes n ON wn.node_id = n.id
WHERE ST_DWithin(
    w.geom::geography,
    ST_SetSRID(ST_MakePoint(@lon, @lat), 4326)::geography,
    @radius
)
ORDER BY RANDOM()
LIMIT @count
```

**Key performance aspects:**
1. **Filter ways first** (fewer objects than nodes) using GiST index on `planet_osm_ways.geom`
2. **Join to nodes** only for ways in the radius (avoids filtering 6M nodes directly)
3. **DISTINCT** prevents duplicates if nodes belong to multiple ways
4. **`::geography` cast** makes the radius accurate in meters

### OSM Data File

Coverage: **Contiguous US + Ontario**

Merged PBF: `us-states-merged.osm.pbf`
- Downloaded from Geofabrik: `us-latest.osm.pbf` + `ontario-latest.osm.pbf`
- Merged with osmium: `osmium merge us-latest.osm.pbf ontario-latest.osm.pbf -o us-states-merged.osm.pbf`
- Used by osm2pgsql for node discovery. Routing is now handled by Mapbox APIs.

See `import-states.sh` in the repo root for the import procedure.

### Updating OSM Data (Zero Downtime)

`import-states.sh` uses a blue/green table swap for all three tables:
1. Import into `planet_osm_nodes_new`, `planet_osm_ways_new`, `planet_osm_way_nodes_new` via `osm2pgsql-flex-staging.lua`
2. Atomically rename all three tables in a single transaction:
   - `planet_osm_nodes` → `planet_osm_nodes_old`; `_new` → active
   - `planet_osm_ways` → `planet_osm_ways_old`; `_new` → active
   - `planet_osm_way_nodes` → `planet_osm_way_nodes_old`; `_new` → active
3. Drop all `_old` tables
4. Create indexes on the new tables

Active game sessions are unaffected — their nodes are stored in `MapNodes`. Only new `/generate` calls hit the OSM tables, and they stay live throughout the atomic swap.

### Import Gotchas

- **Must use `--create` (not `--append`) without `--slim`**: The flex lua script only defines the output tables, not slim node cache tables. `--append` requires `--slim`.
- **Lua script processes ways to identify safe cycling/walking routes**: During way processing, the script evaluates OSM tags (`highway`, `bicycle=no`, `foot=no`) and only stores ways that are safe. Node-way relationships are stored in `planet_osm_way_nodes` for efficient join queries.
- **Use the arm64 image**: `osm2pgsql-arm64:latest` (built locally). `iboates/osm2pgsql` is amd64-only and runs under Rosetta emulation — slower and causes platform warnings.
- **Don't use `iboates/osm2pgsql` with `--slim --append`**: Causes "extra data after last expected column" errors due to version mismatch with the lua script.
- **Kill zombie DB connections before importing**: A stuck importer can leave backend connections holding locks, blocking `DROP TABLE`. Use `pg_terminate_backend()` to clear them.
- **pgsql output is incompatible with flex append**: Don't mix output modes across imports into the same database.

## Routing: Mapbox APIs

Routing is now handled via Mapbox APIs instead of self-hosted GraphHopper/Valhalla.

- **Validation**: `MapboxRoutingService.ValidateNodesAsync()` uses Mapbox Match Service to snap coordinates to the road network
- **Optimization**: `MapboxRoutingService.OptimizeRouteAsync()` uses Mapbox Optimization API to find efficient visit order through up to 12 locations
- **Multi-Node Routing**: `MapboxRoutingService.RouteToMultipleNodesAsync()` orchestrates chunking and chaining for larger node lists
- **Configuration**: Requires `MAPBOX_API_KEY` environment variable (set in `.env`)

## Deployment

All services run via `docker-compose.yml` in the repo root:

| Service | Port | Notes |
|---|---|---|
| `bikeapelago-api` | 8080 | .NET API |
| `bikeapelago-react` | 8182 | Nginx + React SPA |
| `postgis` | 5432 | Shared PostGIS instance (two DBs: `bikeapelago`, `osm_discovery`) |
| `osm-importer` | — | Runs once and exits |

In development, the Vite dev server proxies `/api` to `localhost:5054`.
