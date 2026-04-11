# Adding Elevation Data to MapNodes

This guide covers setting up free elevation data in PostGIS for all nodes in North America.

## Problem
Mapbox free plan doesn't include elevation in routing responses. Previous approach used a rate-limited elevation API. **Better solution**: Load SRTM elevation data once into PostGIS as a raster, then bulk-update all millions of nodes with a single SQL query.

## Solution Overview

1. **Migration**: Add `elevation` column to `MapNodes` table (already done)
2. **Download**: Get free SRTM elevation data covering North America
3. **Load**: Import into PostGIS as a raster table
4. **Update**: Bulk-populate elevation for all nodes via `ST_Value()` query

## Cost
- **SRTM data**: Free (public domain)
- **Processing**: One-time, ~minutes for millions of nodes
- **Storage**: ~100MB-1GB for North America elevation raster
- **After**: Zero API calls—elevation is already in your database

## Prerequisites

Install GDAL tools (handles raster processing):

```bash
# macOS
brew install gdal

# Ubuntu/Debian
sudo apt-get install gdal-bin gdal-data gdal-models

# Docker (already in postgis image)
docker exec postgis-dev gdal_translate --version
```

Verify installation:
```bash
raster2pgsql --version
gdalbuildvrt --version
```

## Step-by-Step Setup

### 1. Apply Database Migration

This adds the `elevation` column and creates the raster table:

```bash
cd /Volumes/1TB/Repos/avarts/bikeapelago-react/api
dotnet ef database update
```

The migration `AddElevationToMapNode` will:
- Add `elevation: int?` column to `MapNodes`
- Create `srtm_elevation` raster table
- Create spatial index on raster

### 2. Download SRTM Data

SRTM provides free 30-meter resolution elevation globally. Choose one source:

#### Option A: USGS (easiest, no account needed initially)
1. Visit https://earthexplorer.usgs.gov
2. Set area: bounding box over your nodes
3. Search dataset: "SRTM 1 Arc-Second Global"
4. Download GeoTIFFs to your machine

#### Option B: OpenDEM (automated, no login)
```bash
# Download tiles for North America
wget -r https://www.opendem.info/raster/SRTM30/SRTM30_srtm/N40W120_srtm.tar.gz
# ... repeat for other tiles
```

#### Option C: Cloud-based (via GDAL)
```bash
# Access SRTM via cloud without downloading
gdalwarp -of COG /vsicurl/https://s3.amazonaws.com/elevation-tiles-prod/... output.tif
```

### 3. Merge GeoTIFFs into Single Raster

If you have multiple tiles, combine them:

```bash
# Download goes to ~/Downloads/srtm/
cd ~/Downloads/srtm

# Extract all tar.gz files
for f in *.tar.gz; do tar -xzf "$f"; done

# Merge all .tif files into one VRT
gdalbuildvrt combined.vrt *.tif

# Optional: Convert VRT to single GeoTIFF (larger file but slightly faster queries)
gdalwarp -of GTiff combined.vrt combined.tif
```

### 4. Load into PostGIS

Use `load_elevation_simple.py`:

```bash
cd /Volumes/1TB/Repos/avarts/bikeapelago-react/scripts

# Install dependencies (psycopg2)
pip install psycopg2-binary

# Load the GeoTIFF
python3 load_elevation_simple.py \
  --host localhost \
  --port 5433 \
  --database bikeapelago \
  --user osm \
  --password osm_secret \
  --geotiff ~/Downloads/srtm/combined.vrt
```

Or load directly with `raster2pgsql`:

```bash
# If SRTM is on your machine
raster2pgsql -I -C -s ~/Downloads/srtm/combined.vrt public.srtm_elevation | \
  PGPASSWORD=osm_secret psql -h localhost -p 5433 -U osm -d bikeapelago

# Or if using Docker (mount volume)
docker exec postgis-dev raster2pgsql -I -C -s /data/combined.vrt public.srtm_elevation | \
  PGPASSWORD=osm_secret psql -h postgis -U osm -d bikeapelago
```

### 5. Verify Raster Loaded

```sql
-- Check raster table
SELECT COUNT(*), ST_AsText(ST_Envelope(ST_Union(rast)))
FROM srtm_elevation;

-- Should show coverage over North America
-- Output: (1, "POLYGON((-130 25, -60 25, -60 50, -130 50, -130 25))")
```

### 6. Query Elevation at Specific Points

Test elevation lookup before bulk update:

```sql
-- Get elevation for a single node
SELECT m.id, m.name, m."Location",
       ST_Value(r.rast, ST_Transform(m."Location", 4326))::int as elevation
FROM "MapNodes" m
CROSS JOIN srtm_elevation r
WHERE ST_Intersects(r.rast, ST_Transform(m."Location", 4326))
LIMIT 5;
```

### 7. Bulk Update All Nodes

The `load_elevation_simple.py` script does this automatically, or run manually:

```sql
UPDATE "MapNodes" m
SET elevation = (ST_Value(r.rast, ST_Transform(m."Location", 4326)))::int
FROM srtm_elevation r
WHERE ST_Intersects(r.rast, ST_Transform(m."Location", 4326))
AND m.elevation IS NULL;
```

This will update **all nodes** in minutes. On a modern machine with good SSD:
- 100K nodes: ~5 seconds
- 1M nodes: ~30 seconds
- 10M nodes: ~3-5 minutes

### 8. Verify Results

```sql
-- Check how many nodes got elevation
SELECT COUNT(*) as total_nodes,
       COUNT(elevation) as nodes_with_elevation,
       AVG(elevation)::int as avg_elevation,
       MIN(elevation) as min_elevation,
       MAX(elevation) as max_elevation
FROM "MapNodes";
```

## Using Elevation in Your API

Once populated, elevation is available in queries:

```csharp
// In your API
var nodes = await dbContext.MapNodes
    .Where(m => m.SessionId == sessionId && m.Elevation.HasValue)
    .Select(m => new {
        m.Id,
        m.Name,
        m.Location,
        m.Elevation
    })
    .ToListAsync();
```

JSON response now includes elevation:
```json
{
  "id": "...",
  "name": "Mountain Peak",
  "lat": 40.5,
  "lon": -120.3,
  "elevation": 2847
}
```

## Troubleshooting

### `raster2pgsql: command not found`
Install GDAL: `brew install gdal`

### Elevation values are NULL after update
1. Check raster is loaded: `SELECT COUNT(*) FROM srtm_elevation;` should return > 0
2. Verify CRS match: Both raster and nodes should use SRID 4326
3. Check spatial overlap: `SELECT ST_Intersects(srtm_raster, node_location) ...`

### Raster doesn't cover my nodes
Download additional SRTM tiles covering the area where your nodes exist. Check with:
```sql
SELECT ST_AsText(ST_Extent("Location")) FROM "MapNodes";
```

### Performance is slow
- Build spatial index if missing: `CREATE INDEX idx_srtm ON srtm_elevation USING GIST(ST_ConvexHull(rast));`
- Use VRT instead of GeoTIFF (VRT is faster to query, larger in RAM)
- Consider tiling large rasters

## Advanced: Tiling for Large Areas

If North America raster is too large, tile it:

```bash
# Split GeoTIFF into 512x512px tiles
gdal_translate -of COG -co TILED=YES -co COMPRESS=deflate \
  combined.tif combined_tiled.tif

# Then load tiled version
raster2pgsql -I combined_tiled.tif | psql -d bikeapelago
```

## References

- **SRTM Data**: https://earthexplorer.usgs.gov/
- **PostGIS Raster**: https://postgis.net/docs/Raster_Intersects.html
- **GDAL**: https://gdal.org/
- **OpenDEM**: https://www.opendem.info/

## Cost Comparison

| Method | Speed | Cost | Complexity |
|--------|-------|------|------------|
| **API per node** | 1 call/node | 💰 Rate-limited, expensive | Simple |
| **PostGIS raster** | 1 bulk query | 🆓 Free (SRTM) | Medium |
| **Cache layer** | Cache hits | 💰 Cache misses use API | Complex |
| **Pre-compute** | Instant lookup | 🆓 Free | Medium |

**Recommendation**: Use PostGIS raster (this approach). One-time setup, instant queries, zero API costs.
