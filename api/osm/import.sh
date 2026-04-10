#!/bin/bash
# Import OSM data into the osm_discovery PostGIS database.
#
# Uses a blue/green table swap for zero-downtime updates:
#   1. Import into planet_osm_nodes_new / planet_osm_ways_new (staging tables)
#   2. Atomically rename staging -> live
#   3. Drop old tables
#
# Active game sessions are unaffected — they use stored MapNodes, not this table.
# Only new /generate calls hit planet_osm_nodes, and they stay live throughout.
#
# Prerequisites:
#   - osm2pgsql-arm64:latest Docker image built:
#       docker build -t osm2pgsql-arm64:latest ./bikeapelago-react/api/osm/osm2pgsql-arm64/
#   - PBF file at graphhopper/data/na-roads-only.osm.pbf
#       Filter from full NA planet: osmium tags-filter north-america-latest.osm.pbf w/highway -o na-roads-only.osm.pbf
#   - PostGIS container running: docker compose up -d postgis
#
# Can be run from any directory — script locates the repo root automatically.

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../../.." && pwd)"

PBF_FILE="$REPO_ROOT/graphhopper/data/na-roads-only.osm.pbf"

if [ ! -f "$PBF_FILE" ] || [ ! -s "$PBF_FILE" ]; then
  echo "ERROR: $PBF_FILE not found or empty."
  echo "Filter from the full NA planet file:"
  echo "  osmium tags-filter north-america-latest.osm.pbf w/highway -o na-roads-only.osm.pbf"
  exit 1
fi

echo "=== Importing $(basename $PBF_FILE) into staging table ==="
echo "    Size: $(du -sh $PBF_FILE | cut -f1)"
echo "    Started: $(date)"

# Drop any leftover staging tables from a previous failed run
docker exec postgis psql -U osm -d osm_discovery \
  -c "DROP TABLE IF EXISTS planet_osm_nodes_new;
      DROP TABLE IF EXISTS planet_osm_ways_new;" 2>/dev/null || true

# Import into staging tables
docker run --rm \
  -v "$REPO_ROOT/graphhopper/data:/data:ro" \
  -v "$SCRIPT_DIR:/import:ro" \
  --network host \
  -e PGPASSWORD=osm_secret \
  --memory=12g \
  --entrypoint osm2pgsql \
  osm2pgsql-arm64:latest \
    --host localhost \
    --port 5432 \
    -d osm_discovery \
    -U osm \
    --output=flex \
    --style /import/osm2pgsql-flex-staging.lua \
    --create \
    --cache=10000 \
    --number-processes=1 \
    /data/na-roads-only.osm.pbf

echo ""
echo "=== Import complete. Performing atomic table swap... ==="

docker exec postgis psql -U osm -d osm_discovery -c "
BEGIN;
  -- Nodes table
  DROP TABLE IF EXISTS planet_osm_nodes_old;
  ALTER TABLE IF EXISTS planet_osm_nodes RENAME TO planet_osm_nodes_old;
  ALTER TABLE planet_osm_nodes_new RENAME TO planet_osm_nodes;

  -- Ways table
  DROP TABLE IF EXISTS planet_osm_ways_old;
  ALTER TABLE IF EXISTS planet_osm_ways RENAME TO planet_osm_ways_old;
  ALTER TABLE planet_osm_ways_new RENAME TO planet_osm_ways;
COMMIT;
"

echo "=== Table swap complete. Dropping old tables... ==="
docker exec postgis psql -U osm -d osm_discovery \
  -c "DROP TABLE IF EXISTS planet_osm_nodes_old;
      DROP TABLE IF EXISTS planet_osm_ways_old;"

echo ""
echo "=== Creating indexes for query performance... ==="
docker exec postgis psql -U osm -d osm_discovery << 'SQL'
  CREATE INDEX IF NOT EXISTS planet_osm_ways_geom_gist ON planet_osm_ways USING GIST (geom);
SQL

echo ""
echo "=== Done! Final row counts:"
docker exec postgis psql -U osm -d osm_discovery \
  -c "SELECT 'Nodes: ' || count(*) FROM planet_osm_nodes UNION ALL SELECT 'Ways: ' || count(*) FROM planet_osm_ways;"

echo ""
echo "=== Way Indexes:"
docker exec postgis psql -U osm -d osm_discovery \
  -c "SELECT indexname FROM pg_indexes WHERE tablename = 'planet_osm_ways';"

echo ""
echo "    Finished: $(date)"
