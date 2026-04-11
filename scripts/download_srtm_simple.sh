#!/bin/bash

# Simple SRTM downloader using public mirrors (no auth needed)
# Downloads SRTM tiles for North America

OUTPUT_DIR="${1:-$HOME/Downloads/srtm_north_america}"
REGION="${2:-ALL}"

mkdir -p "$OUTPUT_DIR"

echo "🌍 Downloading SRTM elevation tiles for North America"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "Output: $OUTPUT_DIR"
echo "Region: $REGION"
echo ""

# SRTM tiles for North America
# Format: N##W###
declare -a TILES=(
  # USA - East
  "N40W080" "N40W075" "N42W078" "N41W075" "N43W074" "N44W074"

  # USA - Central
  "N40W105" "N40W100" "N41W100" "N41W095" "N35W100" "N35W095" "N35W090"

  # USA - West
  "N40W120" "N40W115" "N42W124" "N42W120" "N40W124" "N37W124" "N37W120"

  # Canada
  "N50W120" "N50W100" "N50W080" "N45W080" "N45W090" "N45W100"

  # Mexico
  "N32W117" "N32W115" "N30W115" "N25W110"
)

# Download from USGS public access
BASE_URL="https://lpdaac.usgs.gov/data_ftp/SRTM_GL1/SRTM_GL1_srtm"

DOWNLOADED=0
FAILED=0

for TILE in "${TILES[@]}"; do
  FILENAME="${TILE}_srtm.zip"
  OUTPUT_FILE="$OUTPUT_DIR/$FILENAME"

  if [ -f "$OUTPUT_FILE" ]; then
    echo "✓ $TILE (already downloaded)"
    continue
  fi

  echo -n "⏳ $TILE... "

  # Try download
  if curl -s -f -o "$OUTPUT_FILE" --max-time 30 \
    "$BASE_URL/${TILE}_srtm/${FILENAME}" 2>/dev/null; then
    echo "✓"
    ((DOWNLOADED++))
  else
    echo "✗"
    rm -f "$OUTPUT_FILE"
    ((FAILED++))
  fi
done

echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "Downloaded: $DOWNLOADED tiles"
echo "Failed: $FAILED tiles"
echo ""

if [ $DOWNLOADED -gt 0 ]; then
  echo "✓ Next steps:"
  echo "  1. Extract: cd $OUTPUT_DIR && unzip '*.zip'"
  echo "  2. Merge: gdalbuildvrt combined.vrt *.tif"
  echo "  3. Load: python3 load_elevation_simple.py --geotiff combined.vrt"
else
  echo "⚠️  No tiles downloaded. Trying alternative sources..."
  echo ""
  echo "Manual option:"
  echo "  1. Visit: https://earthexplorer.usgs.gov/"
  echo "  2. Login with your USGS account"
  echo "  3. Search your area, filter: 'SRTM 1 Arc-Second Global DEM'"
  echo "  4. Download tiles"
  echo "  5. Extract and load with load_elevation_simple.py"
fi
