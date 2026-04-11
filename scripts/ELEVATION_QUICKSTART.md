# Elevation Setup - Quick Start

## What's Done
✅ Added `elevation` column to MapNodes model
✅ Created EF Core migration: `20260411163808_AddElevationToMapNode`
✅ Created 3 elevation loading scripts
✅ Created full setup guide

## Quick Path Forward

### 1. Apply Migration
```bash
cd api
dotnet ef database update
```

### 2. Download SRTM Data (Choose One)

**Easiest: USGS**
- Go to https://earthexplorer.usgs.gov
- Search "SRTM 1 Arc-Second Global"
- Download tiles covering your area (saves to `~/Downloads/srtm/`)

**Automated: Via GDAL+curl** (if you have GDAL installed)
```bash
# Quick download of US sample
mkdir -p ~/Downloads/srtm && cd ~/Downloads/srtm
gdalwarp /vsicurl/https://cloud.sdsc.edu/v1/AUTH_opentopography/Raster/SRTM_GL30/SRTM_GL30_srtm/SRTM_GL30_srtm_srtm.tar.gz combined.vrt
```

### 3. Install GDAL (if not already)
```bash
brew install gdal  # macOS
# or
sudo apt-get install gdal-bin  # Ubuntu
```

### 4. Merge tiles (if you have multiple)
```bash
cd ~/Downloads/srtm
for f in *.tar.gz; do tar -xzf "$f"; done
gdalbuildvrt combined.vrt *.tif
```

### 5. Load Into PostGIS
```bash
cd /Volumes/1TB/Repos/avarts/bikeapelago-react/scripts

# Option A: Python script (recommended)
pip install psycopg2-binary
python3 load_elevation_simple.py --geotiff ~/Downloads/srtm/combined.vrt

# Option B: Command line
raster2pgsql -I -C -s ~/Downloads/srtm/combined.vrt public.srtm_elevation | \
  PGPASSWORD=osm_secret psql -h localhost -p 5433 -U osm -d bikeapelago
```

### 6. Verify & Bulk Update
```bash
# Check
psql -h localhost -p 5433 -U osm -d bikeapelago << SQL
SELECT COUNT(*) FROM srtm_elevation;
SQL

# Bulk update happens automatically in load_elevation_simple.py
# Or manually run:
psql -h localhost -p 5433 -U osm -d bikeapelago << SQL
UPDATE "MapNodes" m
SET elevation = (ST_Value(r.rast, ST_Transform(m."Location", 4326)))::int
FROM srtm_elevation r
WHERE ST_Intersects(r.rast, ST_Transform(m."Location", 4326))
AND m.elevation IS NULL;
SQL
```

### 7. Check Results
```bash
psql -h localhost -p 5433 -U osm -d bikeapelago << SQL
SELECT COUNT(*) as total,
       COUNT(elevation) as with_elevation,
       AVG(elevation)::int as avg_elevation
FROM "MapNodes";
SQL
```

## Files Created
- `load_elevation_simple.py` - Main loader script (Python)
- `load_elevation_docker.sh` - Docker-based approach (Bash)
- `load_elevation.py` - Full SRTM downloader (Python, complex)
- `ELEVATION_SETUP.md` - Comprehensive guide

## Next Steps
1. Download SRTM GeoTIFFs
2. Run migration
3. Load raster using Python script
4. Done—elevation is now in MapNodes!

## After Setup
API code can now access elevation:
```csharp
var node = await context.MapNodes.FirstAsync(m => m.Id == id);
var elevation = node.Elevation; // Now populated!
```

JSON response includes elevation automatically.

## Questions?
See `ELEVATION_SETUP.md` for detailed walkthrough and troubleshooting.
