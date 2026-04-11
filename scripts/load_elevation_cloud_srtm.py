#!/usr/bin/env python3
"""
Load elevation data from cloud-hosted SRTM sources directly into PostGIS.
No downloading needed - reads from AWS S3 / cloud sources via GDAL.

This approach:
1. Uses GDAL to read SRTM tiles from cloud
2. Creates virtual raster (VRT) from cloud sources
3. Loads directly into PostGIS

Requires: GDAL with cloud support (gdal_translate, gdalwarp, gdalbuildvrt)
"""

import subprocess
import json
from pathlib import Path
import logging
import psycopg2
from psycopg2.extensions import ISOLATION_LEVEL_AUTOCOMMIT

logging.basicConfig(level=logging.INFO, format='%(levelname)s: %(message)s')
logger = logging.getLogger(__name__)

# SRTM tiles for North America (as VRT paths from cloud)
# These use GDAL's /vsicurl/ to read directly from USGS cloud
SRTM_CLOUD_URLS = {
    # Format: tile_code: cloud_url
    # Using USGS public data on cloud
    'N40W080': '/vsicurl/https://cloud.sdsc.edu/v1/AUTH_opentopography/Raster/SRTM_GL30/SRTM_GL30_srtm/N40W080_srtm.tar.gz',
    'N40W075': '/vsicurl/https://cloud.sdsc.edu/v1/AUTH_opentopography/Raster/SRTM_GL30/SRTM_GL30_srtm/N40W075_srtm.tar.gz',
    'N35W080': '/vsicurl/https://cloud.sdsc.edu/v1/AUTH_opentopography/Raster/SRTM_GL30/SRTM_GL30_srtm/N35W080_srtm.tar.gz',
    'N35W075': '/vsicurl/https://cloud.sdsc.edu/v1/AUTH_opentopography/Raster/SRTM_GL30/SRTM_GL30_srtm/N35W075_srtm.tar.gz',
}

def create_cloud_vrt(output_vrt: Path) -> bool:
    """Create VRT from cloud SRTM sources using GDAL."""
    try:
        logger.info("Creating virtual raster from cloud SRTM sources...")

        # Build list of cloud URLs
        cloud_sources = list(SRTM_CLOUD_URLS.values())

        # Use gdalbuildvrt to combine cloud sources
        cmd = ['gdalbuildvrt', '-overwrite', str(output_vrt)] + cloud_sources

        logger.info(f"Running: {' '.join(cmd[:3])} [cloud sources]...")
        result = subprocess.run(cmd, capture_output=True, text=True, timeout=300)

        if result.returncode != 0:
            logger.error(f"gdalbuildvrt failed: {result.stderr}")
            return False

        logger.info(f"✓ VRT created: {output_vrt}")

        # Verify VRT
        result = subprocess.run(['gdalinfo', str(output_vrt)], capture_output=True, text=True)
        if 'Size is' in result.stdout:
            logger.info("✓ VRT is valid and readable")
            return True
        else:
            logger.error("VRT created but may not be valid")
            return False

    except subprocess.TimeoutExpired:
        logger.error("Cloud VRT creation timed out (network issue?)")
        return False
    except FileNotFoundError as e:
        logger.error(f"GDAL tool not found: {e}. Install with: brew install gdal")
        return False
    except Exception as e:
        logger.error(f"Failed to create cloud VRT: {e}")
        return False

def load_vrt_to_postgis(vrt_path: Path, db_host: str, db_port: int,
                       db_name: str, db_user: str, db_password: str) -> bool:
    """Load VRT into PostGIS using raster2pgsql."""
    try:
        logger.info("Preparing raster data from cloud VRT...")

        # Use raster2pgsql to generate SQL
        r2p_cmd = [
            'raster2pgsql',
            '-I',  # Create spatial index
            '-C',  # Create constraints
            '-e',  # Estimate extent
            '-s',  # Skip NULL values
            str(vrt_path),
            'public.srtm_elevation'
        ]

        logger.info("Generating raster SQL...")
        result = subprocess.run(r2p_cmd, capture_output=True, text=True, timeout=600)

        if result.returncode != 0:
            logger.error(f"raster2pgsql failed: {result.stderr}")
            return False

        sql_content = result.stdout
        logger.info(f"✓ SQL generated ({len(sql_content)/1024:.1f}KB)")

        # Load into PostGIS
        logger.info("Loading raster into PostGIS...")
        try:
            conn = psycopg2.connect(
                host=db_host,
                port=db_port,
                database=db_name,
                user=db_user,
                password=db_password
            )
            conn.set_isolation_level(ISOLATION_LEVEL_AUTOCOMMIT)
            cur = conn.cursor()

            # Execute SQL
            cur.execute(sql_content)

            logger.info("✓ Raster loaded into PostGIS")
            cur.close()
            conn.close()
            return True

        except Exception as e:
            logger.error(f"Failed to load into PostgreSQL: {e}")
            return False

    except subprocess.TimeoutExpired:
        logger.error("Raster generation timed out")
        return False
    except Exception as e:
        logger.error(f"Failed to load VRT: {e}")
        return False

def bulk_update_elevation(db_host: str, db_port: int, db_name: str,
                         db_user: str, db_password: str) -> bool:
    """Bulk update all MapNodes with elevation from raster."""
    try:
        logger.info("Bulk updating MapNodes with elevation...")

        conn = psycopg2.connect(
            host=db_host,
            port=db_port,
            database=db_name,
            user=db_user,
            password=db_password
        )
        conn.set_isolation_level(ISOLATION_LEVEL_AUTOCOMMIT)
        cur = conn.cursor()

        sql = """
        UPDATE "MapNodes" m
        SET "Elevation" = (ST_Value(r.rast, ST_Transform(m."Location", 4326)))::int
        FROM srtm_elevation r
        WHERE ST_Intersects(r.rast, ST_Transform(m."Location", 4326))
        AND m."Elevation" IS NULL;
        """

        cur.execute(sql)
        rows_updated = cur.rowcount

        logger.info(f"✓ Updated {rows_updated} nodes with elevation")

        # Get stats
        cur.execute("""
        SELECT COUNT(*) as total,
               COUNT("Elevation") as with_elevation,
               ROUND(AVG("Elevation")::numeric, 1) as avg_elev,
               MIN("Elevation") as min_elev,
               MAX("Elevation") as max_elev
        FROM "MapNodes"
        WHERE "Elevation" IS NOT NULL;
        """)

        total, with_elev, avg_elev, min_elev, max_elev = cur.fetchone()
        logger.info(f"Elevation stats:")
        logger.info(f"  Nodes with elevation: {with_elev}/{total}")
        logger.info(f"  Average: {avg_elev}m")
        logger.info(f"  Range: {min_elev}m - {max_elev}m")

        cur.close()
        conn.close()
        return True

    except Exception as e:
        logger.error(f"Failed to update elevation: {e}")
        return False

def main():
    import argparse

    parser = argparse.ArgumentParser(
        description='Load elevation from cloud SRTM directly into PostGIS',
        epilog='This reads SRTM data from cloud storage (no download needed)'
    )
    parser.add_argument('--host', default='localhost')
    parser.add_argument('--port', type=int, default=5433)
    parser.add_argument('--database', default='bikeapelago')
    parser.add_argument('--user', default='osm')
    parser.add_argument('--password', default='osm_secret')
    parser.add_argument('--vrt-dir', default='/tmp', help='Directory for VRT file')

    args = parser.parse_args()

    vrt_path = Path(args.vrt_dir) / 'srtm_cloud.vrt'

    logger.info("=== Cloud-based SRTM Elevation Loader ===")
    logger.info(f"Database: {args.database}@{args.host}:{args.port}")
    logger.info("")

    # Step 1: Create cloud VRT
    if not create_cloud_vrt(vrt_path):
        logger.error("Failed to create cloud VRT")
        return 1

    # Step 2: Load into PostGIS
    if not load_vrt_to_postgis(vrt_path, args.host, args.port, args.database,
                              args.user, args.password):
        logger.error("Failed to load into PostGIS")
        return 1

    # Step 3: Bulk update nodes
    if not bulk_update_elevation(args.host, args.port, args.database,
                                args.user, args.password):
        logger.error("Failed to update elevation")
        return 1

    logger.info("")
    logger.info("✅ Elevation data successfully loaded from cloud!")
    return 0

if __name__ == '__main__':
    import sys
    sys.exit(main())
