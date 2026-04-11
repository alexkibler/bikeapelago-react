#!/usr/bin/env python3
"""
Load SRTM elevation data into PostGIS and populate MapNodes with elevation values.

Downloads SRTM 30m elevation tiles for North America, loads them into PostGIS as
a raster table, then bulk-updates all MapNodes with elevation via spatial query.

Usage:
    python3 load_elevation.py [--db-host localhost] [--db-port 5433] [--db-name bikeapelago]
"""

import os
import sys
import argparse
import urllib.request
import tarfile
import tempfile
import subprocess
from pathlib import Path
import logging

logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(levelname)s - %(message)s')
logger = logging.getLogger(__name__)

# SRTM tile coordinates for North America (simplified - covers most bike routes)
# Format: (N/S, lat, E/W, lon)
SRTM_TILES = [
    # United States (main coverage)
    ('N', '40', 'W', '120'),  # Northwest
    ('N', '40', 'W', '100'),  # North-central
    ('N', '40', 'W', '080'),  # Northeast
    ('N', '35', 'W', '120'),  # California
    ('N', '35', 'W', '100'),  # Central
    ('N', '35', 'W', '080'),  # East
    ('N', '30', 'W', '100'),  # Southwest/South-central
    ('N', '30', 'W', '080'),  # Southeast
    ('N', '25', 'W', '080'),  # Florida
    # Canada (sample)
    ('N', '50', 'W', '120'),
    ('N', '45', 'W', '080'),
]

USGS_BASE_URL = "https://e4ftl01.cr.usgs.gov/LE07/SRTM30M/SRTM30M_srtm"

def download_srtm_tile(tile_code: str, output_dir: Path) -> Path | None:
    """Download a single SRTM tile from USGS."""
    url = f"{USGS_BASE_URL}/{tile_code}/{tile_code}_srtm.tar.gz"
    output_file = output_dir / f"{tile_code}.tar.gz"

    if output_file.exists():
        logger.info(f"Tile {tile_code} already downloaded")
        return output_file

    try:
        logger.info(f"Downloading {tile_code}...")
        urllib.request.urlretrieve(url, output_file)
        logger.info(f"Successfully downloaded {tile_code}")
        return output_file
    except Exception as e:
        logger.error(f"Failed to download {tile_code}: {e}")
        return None

def extract_srtm_tile(tar_path: Path, output_dir: Path) -> Path | None:
    """Extract SRTM tar.gz to get GeoTIFF file."""
    try:
        with tarfile.open(tar_path) as tar:
            members = tar.getmembers()
            tif_member = next((m for m in members if m.name.endswith('.tif')), None)
            if tif_member:
                tar.extract(tif_member, output_dir)
                return output_dir / tif_member.name
    except Exception as e:
        logger.error(f"Failed to extract {tar_path}: {e}")
    return None

def create_vrt(geotiff_files: list[Path], output_vrt: Path) -> bool:
    """Create a VRT (Virtual) file combining multiple GeoTIFFs."""
    try:
        logger.info(f"Creating VRT from {len(geotiff_files)} files...")
        gdalbuildvrt_cmd = ['gdalbuildvrt', str(output_vrt)] + [str(f) for f in geotiff_files]
        subprocess.run(gdalbuildvrt_cmd, check=True, capture_output=True)
        logger.info(f"VRT created: {output_vrt}")
        return True
    except subprocess.CalledProcessError as e:
        logger.error(f"Failed to create VRT: {e.stderr.decode()}")
        return False
    except FileNotFoundError:
        logger.error("gdalbuildvrt not found. Install GDAL: brew install gdal")
        return False

def load_raster_to_postgis(vrt_path: Path, db_host: str, db_port: int,
                           db_name: str, db_user: str, db_password: str) -> bool:
    """Load VRT raster into PostGIS using raster2pgsql."""
    try:
        logger.info("Loading raster into PostGIS...")

        # Build raster2pgsql command
        r2p_cmd = [
            'raster2pgsql',
            '-I',  # Create spatial index
            '-C',  # Create constraints
            '-e',  # Estimate extent
            '-s',  # Skip NULL values
            str(vrt_path),
            'public.srtm_elevation'
        ]

        psql_cmd = [
            'psql',
            '-h', db_host,
            '-p', str(db_port),
            '-U', db_user,
            '-d', db_name,
            '-X',  # Don't read .psqlrc
        ]

        env = os.environ.copy()
        env['PGPASSWORD'] = db_password

        # Run: raster2pgsql | psql
        logger.info("Running raster2pgsql -> psql pipeline...")
        r2p = subprocess.Popen(r2p_cmd, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
        psql = subprocess.Popen(psql_cmd, stdin=r2p.stdout, stdout=subprocess.PIPE,
                               stderr=subprocess.PIPE, env=env)
        r2p.stdout.close()

        psql_out, psql_err = psql.communicate()

        if psql.returncode == 0:
            logger.info("Raster successfully loaded into PostGIS")
            return True
        else:
            logger.error(f"psql error: {psql_err.decode()}")
            return False
    except FileNotFoundError as e:
        logger.error(f"Required tool not found: {e}. Make sure GDAL tools are installed.")
        return False
    except Exception as e:
        logger.error(f"Failed to load raster: {e}")
        return False

def update_mapnodes_elevation(db_host: str, db_port: int, db_name: str,
                             db_user: str, db_password: str) -> int:
    """Bulk update MapNodes with elevation from raster."""
    try:
        logger.info("Bulk updating MapNodes with elevation data...")

        sql = """
        UPDATE "MapNodes" m
        SET elevation = (ST_Value(r.rast, ST_Transform(m."Location", 4326)))::int
        FROM srtm_elevation r
        WHERE ST_Intersects(r.rast, ST_Transform(m."Location", 4326))
        AND m.elevation IS NULL;
        """

        psql_cmd = [
            'psql',
            '-h', db_host,
            '-p', str(db_port),
            '-U', db_user,
            '-d', db_name,
            '-c', sql,
            '-X',
        ]

        env = os.environ.copy()
        env['PGPASSWORD'] = db_password

        result = subprocess.run(psql_cmd, capture_output=True, env=env)

        if result.returncode == 0:
            # Parse the UPDATE result
            output = result.stderr.decode()
            logger.info(f"Elevation update completed: {output.strip()}")
            return result.returncode
        else:
            logger.error(f"psql error: {result.stderr.decode()}")
            return result.returncode
    except Exception as e:
        logger.error(f"Failed to update MapNodes: {e}")
        return 1

def main():
    parser = argparse.ArgumentParser(description='Load SRTM elevation data into PostGIS')
    parser.add_argument('--db-host', default='localhost', help='Database host')
    parser.add_argument('--db-port', type=int, default=5433, help='Database port')
    parser.add_argument('--db-name', default='bikeapelago', help='Database name')
    parser.add_argument('--db-user', default='osm', help='Database user')
    parser.add_argument('--db-password', default='osm_secret', help='Database password')
    parser.add_argument('--tiles-dir', default='/tmp/srtm_tiles', help='Directory for SRTM tiles')
    parser.add_argument('--skip-download', action='store_true', help='Skip download if tiles exist')

    args = parser.parse_args()

    tiles_dir = Path(args.tiles_dir)
    tiles_dir.mkdir(parents=True, exist_ok=True)

    # Step 1: Download tiles
    if not args.skip_download:
        logger.info(f"Downloading SRTM tiles to {tiles_dir}")
        geotiffs = []
        for ns, lat, ew, lon in SRTM_TILES:
            tile_code = f"{ns}{lat}{ew}{lon}"
            tar_file = download_srtm_tile(tile_code, tiles_dir)
            if tar_file:
                tif = extract_srtm_tile(tar_file, tiles_dir)
                if tif:
                    geotiffs.append(tif)
                tar_file.unlink()  # Clean up tar

        if not geotiffs:
            logger.error("No tiles downloaded successfully")
            return 1

        logger.info(f"Extracted {len(geotiffs)} GeoTIFF files")
    else:
        # Find existing GeoTIFFs
        geotiffs = list(tiles_dir.glob('**/*.tif'))
        if not geotiffs:
            logger.error("No GeoTIFF files found. Download tiles first.")
            return 1
        logger.info(f"Found {len(geotiffs)} existing GeoTIFF files")

    # Step 2: Create VRT
    vrt_path = tiles_dir / 'srtm_combined.vrt'
    if not create_vrt(geotiffs, vrt_path):
        return 1

    # Step 3: Load into PostGIS
    if not load_raster_to_postgis(vrt_path, args.db_host, args.db_port, args.db_name,
                                  args.db_user, args.db_password):
        return 1

    # Step 4: Update MapNodes
    if update_mapnodes_elevation(args.db_host, args.db_port, args.db_name,
                                 args.db_user, args.db_password) != 0:
        return 1

    logger.info("✓ Elevation data successfully loaded and applied!")
    return 0

if __name__ == '__main__':
    sys.exit(main())
