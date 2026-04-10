# Bikeapelago: .NET 10 API Backend

ASP.NET Core 10 backend providing session management, OSM node generation, authentication, and proxy services.

## Key Technologies

- **ASP.NET Core 10**: Web framework
- **Entity Framework Core**: ORM with PostgreSQL/PostGIS via Npgsql + NetTopologySuite
- **YARP**: Reverse proxy for GraphHopper
- **JWT**: Stateless authentication

## Project Structure

- `Controllers/`: Thin HTTP endpoints — Auth, Sessions, Nodes
- `Data/`: EF Core DbContext and model configuration
- `Models/`: Entities (GameSession, MapNode, User, etc.)
- `Repositories/`: Data access — EF Core (prod) + Mock (tests)
- `Services/`: Business logic — NodeGenerationService, OsmDiscoveryService variants

---

## OSM Node Generation Architecture

This is the core of the routing system. When a session calls `POST /api/sessions/{id}/generate`, the API generates a set of GPS coordinates that are guaranteed to lie on real, routable roads for the requested travel mode (bike or walk). Here is the full pipeline.

### Data Sources

There are two independent spatial databases that must stay in sync:

| System | Purpose | Data Source |
|---|---|---|
| **PostGIS** (`osm_discovery` DB) | Stores OSM road geometries as linestrings. Queried to find candidate node coordinates. | `na-roads-only.osm.pbf` — filtered from the full North America OSM planet file |
| **GraphHopper** | Routing engine. Validates that candidate coordinates are actually routable and snaps them to the road network. | Same `na-roads-only.osm.pbf` |

Both must be built from the same source PBF. If they diverge (e.g. GH is built from an older regional extract), nodes from PostGIS will land outside the GH routing graph and return HTTP 400 from `/nearest`.

### PostGIS Schema

Two tables are populated by `import-states.sh` via `osm2pgsql` with the flex Lua style at `osm-random-node-api/db/import/osm2pgsql-flex-staging.lua`:

**`planet_osm_nodes`** — physical OSM nodes (all, unfiltered)
```
way_id  bigint  PK
id      bigint
geom    geometry(Point, 4326)
```
GiST index on `geom`.

**`planet_osm_ways`** — highway linestrings, pre-filtered to routable roads only
```
way_id       bigint  PK
id           bigint
geom         geometry(LineString, 4326)
cycling_safe boolean   -- true if safe/legal for bikes
walking_safe boolean   -- true if safe/legal for walkers
```
GiST index on `geom`.

The Lua script computes `cycling_safe` and `walking_safe` at import time by checking `highway=` tags and exclusion rules (motorways, `bicycle=no`, `foot=no`). No hstore `tags` column is stored — the booleans are the only filter criteria needed at query time.

### The `ST_DumpPoints` Query

`PostGisOsmDiscoveryService.FetchNodesForSubTargetsAsync` runs a single SQL query that:

1. **Unnests** an array of random sub-target points (probe locations scattered across the requested radius) into a set of `target_geom` rows.
2. **Finds linestrings** near each probe using a `CROSS JOIN LATERAL` with `ST_DWithin` — this forces the planner to use the GiST index per sub-target (index-nested-loop), avoiding a full-table hash join.
3. **Filters by mode** using the pre-computed boolean columns (`cycling_safe` or `walking_safe`).
4. **Dumps physical vertices** out of each matching linestring using `ST_DumpPoints` — these are the actual OSM node coordinates that lie on roads.
5. **Deduplicates, randomizes, and caps** the result set.

```sql
WITH sub_targets AS (
    SELECT ST_SetSRID(ST_MakePoint(lon, lat), 4326) AS target_geom
    FROM unnest(@lons, @lats) AS t(lon, lat)
),
valid_lines AS (
    SELECT DISTINCT w.geom
    FROM sub_targets st
    CROSS JOIN LATERAL (
        SELECT w.geom
        FROM planet_osm_ways w
        WHERE ST_DWithin(w.geom, st.target_geom, @sub_radius_degrees)
          AND ((@is_walk AND w.walking_safe) OR (NOT @is_walk AND w.cycling_safe))
    ) w
),
dumped_points AS (
    SELECT (ST_DumpPoints(geom)).geom AS pt_geom
    FROM valid_lines
)
SELECT x, y FROM (
    SELECT DISTINCT
        ST_X(pt_geom)::float8 AS x,
        ST_Y(pt_geom)::float8 AS y
    FROM dumped_points
) deduped
ORDER BY RANDOM()
LIMIT @safety_cap;
```

**Why `ST_DumpPoints` instead of `planet_osm_nodes`?**
The original approach joined `planet_osm_ways` → `planet_osm_way_nodes` → `planet_osm_nodes` using a relational mapping table. This required a separate `planet_osm_way_nodes` table (expensive to populate via `ST_Intersects`) and added a join. `ST_DumpPoints` extracts vertices directly from the linestring geometry — same data, no intermediate table, simpler schema.

### Sub-Target Probe Strategy

Rather than querying a single large circle, the service generates `ceil(nodeCount × 2.5)` random sub-target points scattered across the full radius. Each probe covers a small sub-radius (clamped between 200m and 1500m). This:

- Ensures geographic spread across the full play area (not clustered near the center)
- Lets the GIST index do fast point-radius lookups rather than one enormous spatial scan
- Provides a buffer of candidates — `2.5×` the needed count, since some probes land in parks, water, or other road-free areas

The `densityBias` parameter controls clustering: `0.5` = uniform area distribution, `1.0` = heavy center clustering.

### Performance

All coordinates are stored and queried in SRID 4326 (WGS84 degrees) — no `ST_Transform` in the hot path. The `sub_radius_degrees` parameter is pre-computed in C# (`meters / 111000.0`) before the query.

Observed end-to-end generate times at 58M ways (North America):

| Node count | Wall time |
|---|---|
| 50 | ~860ms |
| 100 | 700–2,100ms |
| 200 | ~1,400ms |
| 500 | ~1,000ms |
| 1,000 | ~1,500–2,000ms |

The dominant cost is the LATERAL GIST lookups (scales with probe count, not node count). Actual node retrieval and bulk insert are negligible by comparison.

### Import Pipeline

Nodes and ways are imported via a blue/green table swap for zero-downtime updates:

```
na-roads-only.osm.pbf
        │
        ▼
    osm2pgsql (flex, Lua style)
        │
        ├── planet_osm_nodes_new   (staging)
        └── planet_osm_ways_new    (staging)
                │
                ▼ (atomic rename in one transaction)
        planet_osm_nodes  ◄── live, queried by API
        planet_osm_ways   ◄── live, queried by API
```

The filtered PBF is generated from the full NA planet file with:
```bash
osmium tags-filter north-america-latest.osm.pbf w/highway \
  -o na-roads-only.osm.pbf
```
This strips ~75% of the raw data (nodes with no highway tag), taking 18GB → ~4.5GB.

Run the full import from the repo root:
```bash
bash import-states.sh
```

### GraphHopper Rebuild

GraphHopper must be rebuilt whenever the source PBF changes. The graph-cache lives at `graphhopper/data/graph-cache/`. To rebuild:

```bash
docker compose stop graphhopper
rm -rf graphhopper/data/graph-cache
docker compose up -d graphhopper   # GH detects missing cache, rebuilds from PBF_FILE
```

Rebuild time from `na-roads-only.osm.pbf` (~4.5GB): approximately 20–40 minutes depending on hardware.

### Service Selection

Controlled by config at startup (in priority order):

1. `USE_MOCK_OVERPASS=true` → in-memory mock (for E2E tests)
2. `ConnectionStrings:OsmDiscovery` → **PostGisOsmDiscoveryService** (use this)
3. `OsmDiscovery:PbfPath` → PbfOsmDiscoveryService
4. Fallback → OverpassOsmDiscoveryService (external HTTP — avoid in prod, causes 504s)

### Validation

`POST /api/discovery/validate-nodes` proxies a batch of coordinates to GraphHopper's `/nearest` endpoint and checks snap distance. A snap > 50m indicates the node landed on a road type not recognized by GH for the requested vehicle profile (e.g., a motorway for a bike request).

The integration test suite at `api.Tests/Integration/GraphHopperNodeValidationTests.cs` runs full generate + snap audits across 37 North American cities at multiple radii and node counts.

---

## Configuration

### `appsettings.Development.json`

```json
{
  "ConnectionStrings": {
    "OsmDiscovery": "Host=localhost;Port=5432;Database=osm_discovery;Username=osm;Password=osm_secret"
  }
}
```

## Commands

```bash
dotnet run                  # Run the API (localhost:5054 in dev)
dotnet build                # Build
dotnet format               # Format code
dotnet ef database update   # Apply EF Core migrations
```

## API Endpoints

- `POST /api/auth/register` — Register user
- `POST /api/auth/login` — Login, returns JWT
- `GET  /api/sessions` — List sessions for authenticated user
- `POST /api/sessions` — Create session
- `POST /api/sessions/{id}/generate` — Generate map nodes for a session
- `PATCH /api/sessions/{id}` — Update session (AP server URL, slot name)
- `GET  /api/sessions/{id}/nodes` — Get nodes for a session
- `PATCH /api/nodes/{id}` — Update node state (Hidden/Available/Checked)
- `POST /api/discovery/validate-nodes` — Validate coordinates against GraphHopper

## Node Generation Request

`POST /api/sessions/{id}/generate` body:
```json
{
  "centerLat": 41.8781,
  "centerLon": -87.6298,
  "radius": 10000,
  "nodeCount": 25,
  "mode": "bike"
}
```

## Reverse Proxy (YARP)

- `/api/gh/**` → GraphHopper routing
