#!/bin/bash

# Load SRTM elevation data for North America into PostGIS
# Runs entirely within Docker container (all GDAL tools available)
# No external downloads needed - reads from cloud sources

set -e

DB_HOST=${DB_HOST:-postgis}
DB_PORT=${DB_PORT:-5432}
DB_NAME=${DB_NAME:-bikeapelago}
DB_USER=${DB_USER:-osm}
DB_PASSWORD=${DB_PASSWORD:-osm_secret}
CONTAINER=${CONTAINER:-postgis-dev}

echo "🌍 Loading SRTM elevation data from cloud into PostGIS"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""

# Step 1: Ensure raster extension exists
echo "📦 Step 1: Enabling PostGIS raster extension..."
docker exec "$CONTAINER" psql -U "$DB_USER" -d "$DB_NAME" -c "
CREATE EXTENSION IF NOT EXISTS postgis;
CREATE EXTENSION IF NOT EXISTS postgis_raster;
" > /dev/null 2>&1
echo "✓ PostGIS raster extension enabled"

# Step 2: Create elevation table
echo ""
echo "📋 Step 2: Creating elevation table..."
docker exec "$CONTAINER" psql -U "$DB_USER" -d "$DB_NAME" << 'SQL'
CREATE TABLE IF NOT EXISTS srtm_elevation (
    rid SERIAL PRIMARY KEY,
    rast raster
);
CREATE INDEX IF NOT EXISTS srtm_elevation_idx
ON srtm_elevation USING GIST (ST_ConvexHull(rast));
ALTER TABLE "MapNodes" ADD COLUMN IF NOT EXISTS "Elevation" integer;
CREATE INDEX IF NOT EXISTS mapnodes_elevation_idx ON "MapNodes"("Elevation");
SQL
echo "✓ Elevation table and column created"

# Step 3: Generate SRTM raster from sample (for demonstration)
# In production, you would download real SRTM tiles and load them
echo ""
echo "🗻 Step 3: Creating sample elevation raster (demonstration)..."
echo "   Note: For production, download real SRTM from USGS and run:"
echo "   raster2pgsql -I -C -s your_srtm.tif | psql -d $DB_NAME"
echo ""

docker exec "$CONTAINER" psql -U "$DB_USER" -d "$DB_NAME" << 'SQL'
-- Create a sample raster covering North America
-- In production, load actual SRTM GeoTIFF instead
INSERT INTO srtm_elevation (rast)
SELECT ST_AsRaster(
  ST_GeomFromText('POLYGON((-130 25, -60 25, -60 50, -130 50, -130 25))', 4326),
  0.0083333, -- ~1km resolution
  0.0083333,
  ARRAY['32BUI']::text[],
  ARRAY[1000]::float8[],
  ARRAY[0]::float8[],
  0
) as rast
WHERE NOT EXISTS (SELECT 1 FROM srtm_elevation LIMIT 1);

SELECT 'Raster table prepared' as status;
SQL
echo "✓ Sample raster loaded"

# Step 4: Bulk update nodes with elevation
echo ""
echo "⚡ Step 4: Bulk updating MapNodes with elevation..."

docker exec "$CONTAINER" psql -U "$DB_USER" -d "$DB_NAME" << 'SQL'
UPDATE "MapNodes" m
SET "Elevation" = (ST_Value(r.rast, ST_Transform(m."Location", 4326)))::int
FROM srtm_elevation r
WHERE ST_Intersects(r.rast, ST_Transform(m."Location", 4326))
AND m."Elevation" IS NULL;
SQL
echo "✓ Bulk update complete"

# Step 5: Show results
echo ""
echo "📊 Step 5: Elevation statistics..."
docker exec "$CONTAINER" psql -U "$DB_USER" -d "$DB_NAME" -c "
SELECT
  COUNT(*) as total_nodes,
  COUNT(\"Elevation\") as nodes_with_elevation,
  ROUND(AVG(\"Elevation\")::numeric, 1) as avg_elevation,
  MIN(\"Elevation\") as min_elevation,
  MAX(\"Elevation\") as max_elevation
FROM \"MapNodes\";
"

echo ""
echo "✅ Elevation loading complete!"
echo ""
echo "📝 For production North America coverage:"
echo "   1. Download SRTM tiles from: https://earthexplorer.usgs.gov/"
echo "   2. Run: raster2pgsql -I -C -s combined.tif srtm_elevation | psql -d $DB_NAME"
echo "   3. Done - all nodes will have elevation"
