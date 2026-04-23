#!/bin/bash
# Purge ways that are now excluded by updated filter logic, without a full reimport.
#
# Uses osmium to extract way IDs matching the new exclusion criteria from the
# existing PBF file, then DELETEs them from planet_osm_ways in PostGIS.
#
# Exclusion criteria added (matches osm2pgsql-flex-staging.lua):
#   - highway=track (all tracks dropped — too many private/farm roads)
#   - highway=service (all service roads dropped — too many private roads)
#   - access=private or access=no
#
# Prerequisites:
#   - osmium-tool installed (brew install osmium-tool)
#   - PBF file at ./data/na-roads-only.osm.pbf (or set PBF_FILE env var)
#   - PostGIS container running: postgis-osm-prod

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PBF_FILE="${PBF_FILE:-$SCRIPT_DIR/data/na-roads-only.osm.pbf}"
mkdir -p /Volumes/1TB/tmp
WORK_DIR="$(TMPDIR=/Volumes/1TB/tmp mktemp -d)"

trap 'rm -rf "$WORK_DIR"' EXIT

if [ ! -f "$PBF_FILE" ] || [ ! -s "$PBF_FILE" ]; then
  echo "ERROR: $PBF_FILE not found or empty."
  exit 1
fi

if ! command -v osmium &>/dev/null; then
  echo "ERROR: osmium-tool not found. Install with: brew install osmium-tool"
  exit 1
fi

echo "=== Extracting excluded ways from PBF... ==="

# Extract ways matching each exclusion criterion separately, then merge.
# -R / --omit-referenced: only output the ways themselves, not their referenced nodes.
osmium tags-filter "$PBF_FILE" \
  "w/highway=track" \
  "w/highway=service" \
  -R -o "$WORK_DIR/excluded-highway.osm.pbf" --overwrite

osmium tags-filter "$PBF_FILE" \
  "w/access=private" \
  "w/access=no" \
  -R -o "$WORK_DIR/excluded-access.osm.pbf" --overwrite

# Merge and deduplicate
osmium merge \
  "$WORK_DIR/excluded-highway.osm.pbf" \
  "$WORK_DIR/excluded-access.osm.pbf" \
  -o "$WORK_DIR/excluded-all.osm" --overwrite

echo "=== Extracting way IDs... ==="

# Parse OSM XML for way IDs → one ID per line CSV for COPY
grep '<way id=' "$WORK_DIR/excluded-all.osm" \
  | sed 's/.*<way id="\([0-9]*\)".*/\1/' \
  | sort -u \
  > "$WORK_DIR/excluded_ids.txt"

COUNT=$(wc -l < "$WORK_DIR/excluded_ids.txt" | tr -d ' ')
echo "    Found $COUNT ways to purge."

if [ "$COUNT" -eq 0 ]; then
  echo "    Nothing to purge."
  exit 0
fi

echo "=== Checking how many are currently in planet_osm_ways... ==="

# Copy the ID list into a temp table inside the container, then JOIN-delete.
# This avoids generating a massive SQL literal.
docker cp "$WORK_DIR/excluded_ids.txt" postgis-osm-prod:/tmp/excluded_ids.txt

docker exec postgis-osm-prod psql -U osm -d osm_discovery -c "
  CREATE TEMP TABLE _excluded_ids (id bigint);
  COPY _excluded_ids (id) FROM '/tmp/excluded_ids.txt';
  SELECT count(*) AS ways_to_delete FROM planet_osm_ways w JOIN _excluded_ids e ON w.id = e.id;
"

echo ""
read -r -p "Proceed with DELETE? [y/N] " CONFIRM
if [[ ! "$CONFIRM" =~ ^[Yy]$ ]]; then
  echo "Aborted."
  exit 0
fi

echo "=== Deleting excluded ways... ==="

docker exec postgis-osm-prod psql -U osm -d osm_discovery -c "
  CREATE TEMP TABLE _excluded_ids (id bigint);
  COPY _excluded_ids (id) FROM '/tmp/excluded_ids.txt';
  DELETE FROM planet_osm_ways WHERE id IN (SELECT id FROM _excluded_ids);
"

echo ""
echo "=== Done. Remaining way count:"
docker exec postgis-osm-prod psql -U osm -d osm_discovery \
  -c "SELECT count(*) FROM planet_osm_ways;"

docker exec postgis-osm-prod rm -f /tmp/excluded_ids.txt
