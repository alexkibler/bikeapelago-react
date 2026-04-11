#!/usr/bin/env python3
"""
Simple elevation loader using OpenElevation API + PostGIS bulk update.
Much simpler than downloading SRTM—uses free hosted elevation service.

For millions of records, this is still rate-limited. The intended solution is:
1. Download SRTM GeoTIFFs manually once
2. Load them into PostGIS as a raster table
3. Use bulk ST_Value() SQL queries to populate elevation

This script handles the SRTM setup workflow.
"""

import json
import psycopg2
import logging
from pathlib import Path

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

def create_elevation_column(host, port, database, user, password):
    """Create elevation column and raster table in PostGIS."""
    try:
        conn = psycopg2.connect(
            host=host,
            port=port,
            database=database,
            user=user,
            password=password
        )
        cur = conn.cursor()

        # Create raster table
        cur.execute("""
        CREATE TABLE IF NOT EXISTS srtm_elevation (
            rid SERIAL PRIMARY KEY,
            rast raster
        );
        CREATE INDEX IF NOT EXISTS srtm_elevation_idx
        ON srtm_elevation USING GIST (ST_ConvexHull(rast));
        """)

        # Add elevation column to MapNodes
        cur.execute("""
        ALTER TABLE "MapNodes" ADD COLUMN IF NOT EXISTS elevation integer;
        CREATE INDEX IF NOT EXISTS mapnodes_elevation_idx ON "MapNodes"(elevation);
        """)

        conn.commit()
        cur.close()
        conn.close()

        logger.info("✓ Elevation table and column created")
        return True
    except Exception as e:
        logger.error(f"Failed to create table: {e}")
        return False

def load_srtm_raster(host, port, database, user, password, geotiff_path):
    """
    Load a GeoTIFF into PostGIS as a raster.
    Assumes geotiff_path is an absolute path to a local GeoTIFF file.

    Prerequisites:
    - raster2pgsql must be installed (comes with PostGIS)
    - GeoTIFF file must be local to the system running this script
    """
    import subprocess
    import os

    try:
        logger.info(f"Loading GeoTIFF: {geotiff_path}")

        # Build raster2pgsql command
        r2p_cmd = [
            'raster2pgsql',
            '-I',  # Create spatial index
            '-C',  # Create constraints
            '-s',  # Skip NULL values
            geotiff_path,
            'public.srtm_elevation'
        ]

        # Build psql command
        psql_cmd = [
            'psql',
            '-h', host,
            '-p', str(port),
            '-U', user,
            '-d', database,
            '-X',
        ]

        env = os.environ.copy()
        env['PGPASSWORD'] = password

        # Pipe: raster2pgsql | psql
        logger.info("Running raster2pgsql → psql pipeline...")
        r2p = subprocess.Popen(r2p_cmd, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
        psql = subprocess.Popen(psql_cmd, stdin=r2p.stdout, stdout=subprocess.PIPE,
                               stderr=subprocess.PIPE, env=env)
        r2p.stdout.close()

        stdout, stderr = psql.communicate()

        if psql.returncode != 0:
            logger.error(f"psql error: {stderr.decode()}")
            return False

        logger.info("✓ Raster loaded successfully")
        return True

    except FileNotFoundError:
        logger.error("raster2pgsql not found. Install with: brew install gdal")
        return False
    except Exception as e:
        logger.error(f"Failed to load raster: {e}")
        return False

def bulk_update_elevation(host, port, database, user, password):
    """Bulk update all MapNodes with elevation from raster."""
    try:
        conn = psycopg2.connect(
            host=host,
            port=port,
            database=database,
            user=user,
            password=password
        )
        cur = conn.cursor()

        # Update nodes where elevation is NULL
        sql = """
        UPDATE "MapNodes" m
        SET elevation = (ST_Value(r.rast, ST_Transform(m."Location", 4326)))::int
        FROM srtm_elevation r
        WHERE ST_Intersects(r.rast, ST_Transform(m."Location", 4326))
        AND m.elevation IS NULL;
        """

        logger.info("Running bulk elevation update...")
        cur.execute(sql)
        rows_updated = cur.rowcount
        conn.commit()
        cur.close()
        conn.close()

        logger.info(f"✓ Updated {rows_updated} nodes with elevation")
        return True

    except Exception as e:
        logger.error(f"Failed to update elevation: {e}")
        return False

if __name__ == '__main__':
    import argparse

    parser = argparse.ArgumentParser(
        description='Load SRTM elevation into PostGIS',
        epilog="""
SRTM Data Sources:
  • USGS: https://earthexplorer.usgs.gov/ (requires account, free tier)
  • OpenDEM: https://www.opendem.info/ (no account needed)
  • OpenTopography: https://cloud.sdsc.edu/v1/AUTH_opentopography/Raster/SRTM_GL30

Workflow:
  1. Download SRTM GeoTIFFs (covering your area)
  2. Merge with gdalbuildvrt: gdalbuildvrt combined.vrt *.tif
  3. Run this script with --geotiff combined.vrt
        """
    )
    parser.add_argument('--host', default='localhost')
    parser.add_argument('--port', type=int, default=5433)
    parser.add_argument('--database', default='bikeapelago')
    parser.add_argument('--user', default='osm')
    parser.add_argument('--password', default='osm_secret')
    parser.add_argument('--geotiff', help='Path to GeoTIFF file to load')

    args = parser.parse_args()

    # Step 1: Create table
    if not create_elevation_column(args.host, args.port, args.database, args.user, args.password):
        exit(1)

    # Step 2: Load raster if provided
    if args.geotiff:
        if not Path(args.geotiff).exists():
            logger.error(f"GeoTIFF not found: {args.geotiff}")
            exit(1)
        if not load_srtm_raster(args.host, args.port, args.database, args.user,
                               args.password, args.geotiff):
            exit(1)
        # Step 3: Bulk update
        if not bulk_update_elevation(args.host, args.port, args.database, args.user, args.password):
            exit(1)
    else:
        logger.info("No GeoTIFF provided. Table created; ready to load data.")
        logger.info("Download SRTM GeoTIFFs and run: python3 load_elevation_simple.py --geotiff combined.vrt")
