# Bikeapelago: .NET 10 API Backend

ASP.NET Core 10 backend providing session management, OSM node generation, authentication, and proxy services.

## Key Technologies

- **ASP.NET Core 10**: Web framework
- **Entity Framework Core**: ORM with PostgreSQL/PostGIS via Npgsql + NetTopologySuite
- **YARP**: Reverse proxy for PocketBase and GraphHopper
- **JWT**: Stateless authentication

## Project Structure

- `Controllers/`: Thin HTTP endpoints — Auth, Sessions, Nodes
- `Data/`: EF Core DbContext and model configuration
- `Models/`: Entities (GameSession, MapNode, User, etc.)
- `Repositories/`: Data access — EF Core (prod) + Mock (tests)
- `Services/`: Business logic — NodeGenerationService, OsmDiscoveryService variants

## Configuration

### `appsettings.Development.json`

Must include the OSM discovery connection string or the API falls back to the Overpass external API (slow, causes 504s):

```json
{
  "ConnectionStrings": {
    "OsmDiscovery": "Host=localhost;Port=5432;Database=osm_discovery;Username=osm;Password=osm_secret"
  }
}
```

### OSM Discovery Service Selection

Controlled by config at startup (in priority order):
1. `USE_MOCK_OVERPASS=true` → in-memory mock (for E2E tests)
2. `ConnectionStrings:OsmDiscovery` → **PostGisOsmDiscoveryService** (use this)
3. `OsmDiscovery:PbfPath` → PbfOsmDiscoveryService
4. Fallback → OverpassOsmDiscoveryService (external HTTP — avoid)

## Commands

```bash
dotnet run          # Run the API (localhost:5054 in dev)
dotnet build        # Build
dotnet format       # Format code
dotnet ef database update  # Apply EF Core migrations
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

## Node Generation

`POST /api/sessions/{id}/generate` body:
```json
{
  "centerLat": 41.8781,
  "centerLon": -87.6298,
  "radius": 10000,
  "nodeCount": 25,
  "mode": "archipelago"
}
```

Flow: fetch nodes from PostGIS → delete old nodes → bulk insert → update session status.

**Performance notes**:
- Bulk insert uses `AddRange` + single `SaveChangesAsync` — never loop with per-row saves
- PostGIS query uses native 4326 geometry — no `ST_Transform` in WHERE clause
- Requires GiST index on `planet_osm_nodes.geom` — see root `import-states.sh`

## Reverse Proxy (YARP)

- `/api/pb/**` → PocketBase (legacy, phasing out)
- `/api/gh/**` → GraphHopper routing
