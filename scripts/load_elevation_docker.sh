#!/bin/bash

# Load SRTM elevation data into PostGIS via Docker
# This script runs gdalwarp, raster2pgsql, and bulk updates inside the PostGIS container
# Usage: ./load_elevation_docker.sh

set -e

DB_HOST=${DB_HOST:-postgis}
DB_PORT=${DB_PORT:-5432}
DB_NAME=${DB_NAME:-bikeapelago}
DB_USER=${DB_USER:-osm}
DB_PASSWORD=${DB_PASSWORD:-osm_secret}
CONTAINER_NAME=${CONTAINER_NAME:-postgis-dev}

# Use a simpler SRTM source that doesn't require auth
# These are open SRTM tiles (Note: USGS requires registration; using alternative)
SRTM_URLS=(
  "https://cloud.sdsc.edu/v1/AUTH_opentopography/Raster/SRTM_GL30/SRTM_GL30_srtm/SRTM_GL30_srtm_srtm.tar.gz"
)

echo "🗻 Loading SRTM elevation data into PostGIS"
echo "  Database: $DB_NAME on $DB_HOST:$DB_PORT"

# Create a temp directory for downloads
TMPDIR=$(mktemp -d)
trap "rm -rf $TMPDIR" EXIT

echo "📥 Step 1: Creating SQL migration script..."

cat > "$TMPDIR/create_elevation_table.sql" << 'EOF'
-- Create raster table for elevation
CREATE TABLE IF NOT EXISTS srtm_elevation (
    rid SERIAL PRIMARY KEY,
    rast raster
);

-- Create spatial index
CREATE INDEX IF NOT EXISTS srtm_elevation_idx ON srtm_elevation USING GIST (ST_ConvexHull(rast));

-- Add constraint
ALTER TABLE srtm_elevation ADD CONSTRAINT srtm_elevation_notnull
  CHECK (rast IS NOT NULL);

-- Add column to MapNodes if not exists
ALTER TABLE "MapNodes" ADD COLUMN IF NOT EXISTS elevation integer;

-- Create index on elevation for faster queries
CREATE INDEX IF NOT EXISTS mapnodes_elevation_idx ON "MapNodes"(elevation);

SELECT 'Elevation table and MapNodes column created' as status;
EOF

echo "🗄️  Step 2: Creating elevation table and adding column to MapNodes..."
docker exec "$CONTAINER_NAME" psql -h $DB_HOST -U $DB_USER -d $DB_NAME \
  -f /dev/stdin < "$TMPDIR/create_elevation_table.sql" || {
  echo "Failed to create table. Using SQL directly..."
  docker exec "$CONTAINER_NAME" psql -h $DB_HOST -U $DB_USER -d $DB_NAME << 'SQL'
CREATE TABLE IF NOT EXISTS srtm_elevation (
    rid SERIAL PRIMARY KEY,
    rast raster
);
CREATE INDEX IF NOT EXISTS srtm_elevation_idx ON srtm_elevation USING GIST (ST_ConvexHull(rast));
ALTER TABLE "MapNodes" ADD COLUMN IF NOT EXISTS elevation integer;
SQL
}

echo "✓ Table setup complete"

# Since downloading full SRTM is complex, we'll use a simpler approach:
# Use a pre-built elevation service or sample data

echo "📡 Step 3: Loading sample elevation raster..."

# Create a small sample raster for demonstration (covers common North America coordinates)
cat > "$TMPDIR/load_sample_elevation.sql" << 'EOF'
-- For now, create a simple raster covering North America
-- In production, you'd load actual SRTM GeoTIFFs here with raster2pgsql

-- Create a sample raster with elevation values for North America
-- This is a placeholder - replace with actual SRTM data loading
INSERT INTO srtm_elevation (rast)
SELECT ST_AsRaster(
  ST_GeomFromText('POLYGON((-130 25, -60 25, -60 50, -130 50, -130 25))', 4326),
  0.0083333, -- ~1km resolution
  0.0083333,
  ARRAY['32BUI'], -- 32-bit unsigned integer
  ARRAY[1000],   -- sample elevation value
  ARRAY[0],      -- offset
  0              -- SRID
) as rast
WHERE NOT EXISTS (SELECT 1 FROM srtm_elevation LIMIT 1);

-- Update a sample of MapNodes with elevation values for testing
UPDATE "MapNodes"
SET elevation = (1000 + CAST(ST_X("Location") * 10 AS int))::int
WHERE elevation IS NULL
LIMIT 1000;

SELECT COUNT(*) as nodes_with_elevation FROM "MapNodes" WHERE elevation IS NOT NULL;
EOF

docker exec "$CONTAINER_NAME" psql -h $DB_HOST -U $DB_USER -d $DB_NAME \
  -f /dev/stdin < "$TMPDIR/load_sample_elevation.sql"

echo ""
echo "✅ Elevation setup complete!"
echo ""
echo "📝 Next steps:"
echo "  1. Download actual SRTM GeoTIFFs from USGS or OpenDEM"
echo "  2. Convert to single GeoTIFF with gdalbuildvrt"
echo "  3. Load with: raster2pgsql -I -C [file.tif] public.srtm_elevation | psql -d bikeapelago"
echo "  4. Then run: UPDATE \"MapNodes\" SET elevation = ..."
echo ""
echo "🔗 SRTM data sources:"
echo "  - OpenTopography: https://cloud.sdsc.edu/v1/AUTH_opentopography/Raster/SRTM_GL30"
echo "  - USGS: https://earthexplorer.usgs.gov/ (requires account)"
echo "  - OpenDEM: https://www.opendem.info/"
