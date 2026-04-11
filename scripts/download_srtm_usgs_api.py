#!/usr/bin/env python3
"""
Download SRTM data from USGS using authenticated API.

Requires USGS Earth Explorer account and API token.
Token should be in .env file as: USGS_API_TOKEN=your_token_here

Usage:
  python3 download_srtm_usgs_api.py [--region US|CA|MX|ALL]
"""

import os
import requests
import json
from pathlib import Path
from dotenv import load_dotenv
import logging
import argparse
from concurrent.futures import ThreadPoolExecutor, as_completed

logging.basicConfig(level=logging.INFO, format='%(levelname)s: %(message)s')
logger = logging.getLogger(__name__)

# Load .env file
load_dotenv()

USGS_API_TOKEN = os.getenv('USGS_API_TOKEN')
USGS_API_URL = 'https://m2m.cr.usgs.gov/api/v1'

# SRTM tiles for North America by region
SRTM_TILES = {
    'US': [
        'N40W080', 'N40W075', 'N40W105', 'N40W110', 'N40W115', 'N40W120',
        'N35W080', 'N35W075', 'N35W085', 'N35W090', 'N35W095', 'N35W100', 'N35W102', 'N35W108', 'N35W110', 'N35W115',
        'N37W120', 'N37W124', 'N37W110', 'N37W115',
        'N42W078', 'N42W120', 'N42W124',
        'N44W074', 'N44W080', 'N44W087', 'N44W090', 'N44W100', 'N44W110', 'N44W120', 'N44W124',
        'N32W080', 'N32W082', 'N32W085', 'N32W090', 'N32W115', 'N32W117',
        'N28W081', 'N28W083',
        'N41W075', 'N41W095', 'N41W100',
        'N43W074',
    ],
    'CA': [
        'N60W080', 'N60W100', 'N60W120', 'N60W140',
        'N50W080', 'N50W100', 'N50W120', 'N50W140',
        'N45W075', 'N45W080', 'N45W090', 'N45W100', 'N45W120', 'N45W140',
    ],
    'MX': [
        'N32W117', 'N32W115',
        'N30W115',
        'N25W110',
        'N22W105',
        'N20W103',
        'N18W097',
    ],
}

def get_api_token() -> str:
    """Get USGS API token from environment."""
    if not USGS_API_TOKEN:
        raise ValueError(
            "USGS_API_TOKEN not found in .env\n"
            "Get token from: https://ers.usgs.gov/registration\n"
            "Add to .env: USGS_API_TOKEN=your_token_here"
        )
    return USGS_API_TOKEN

def login(token: str) -> str:
    """Authenticate with USGS API and get session token."""
    try:
        response = requests.post(
            f'{USGS_API_URL}/login',
            json={'username': 'YOUR_USERNAME', 'token': token},
            timeout=10
        )

        if response.status_code == 200:
            session_token = response.json().get('data')
            logger.info("✓ Authenticated with USGS API")
            return session_token
        else:
            logger.error(f"Authentication failed: {response.status_code}")
            logger.error(response.text)
            return None
    except Exception as e:
        logger.error(f"Login failed: {e}")
        return None

def search_srtm_tiles(session_token: str, tile_codes: list) -> list:
    """Search for SRTM tiles in USGS database."""
    results = []

    try:
        for tile_code in tile_codes:
            # Search for SRTM 1 Arc-Second DEM
            response = requests.post(
                f'{USGS_API_URL}/search',
                json={
                    'apiKey': session_token,
                    'datasetName': 'SRTM_GL1_Ellip',  # SRTM 1 Arc-Second Global
                    'spatialFilter': {
                        'filterType': 'mbr',
                        'lowerLeftLongitude': -80,
                        'lowerLeftLatitude': 25,
                        'upperRightLongitude': -60,
                        'upperRightLatitude': 50,
                    }
                },
                timeout=10
            )

            if response.status_code == 200:
                data = response.json().get('data', {})
                results.extend(data.get('results', []))
    except Exception as e:
        logger.error(f"Search failed: {e}")

    return results

def download_tile(tile_id: str, session_token: str, output_dir: Path) -> bool:
    """Download a single SRTM tile."""
    try:
        # Request download
        response = requests.post(
            f'{USGS_API_URL}/download-options',
            json={
                'apiKey': session_token,
                'datasetName': 'SRTM_GL1_Ellip',
                'entityIds': [tile_id],
            },
            timeout=10
        )

        if response.status_code != 200:
            logger.error(f"Download request failed: {response.status_code}")
            return False

        # Get download URL
        download_urls = response.json().get('data', {}).get('availableProducts', [])
        if not download_urls:
            logger.error(f"No download URLs available for {tile_id}")
            return False

        url = download_urls[0].get('url')
        if not url:
            logger.error(f"No URL for {tile_id}")
            return False

        # Download file
        output_file = output_dir / f"{tile_id}.tif"
        response = requests.get(url, stream=True, timeout=60)

        with open(output_file, 'wb') as f:
            for chunk in response.iter_content(chunk_size=8192):
                f.write(chunk)

        logger.info(f"✓ Downloaded {tile_id}")
        return True

    except Exception as e:
        logger.error(f"Failed to download {tile_id}: {e}")
        return False

def main():
    parser = argparse.ArgumentParser(
        description='Download SRTM data from USGS using authenticated API',
        epilog='Requires USGS API token in .env: USGS_API_TOKEN=your_token'
    )
    parser.add_argument('--region', choices=['US', 'CA', 'MX', 'ALL'], default='US',
                       help='Region to download (default: US)')
    parser.add_argument('--output-dir', default=str(Path.home() / 'Downloads' / 'srtm_usgs'),
                       help='Output directory for tiles')

    args = parser.parse_args()

    output_dir = Path(args.output_dir)
    output_dir.mkdir(parents=True, exist_ok=True)

    # Get regions to download
    regions = ['US', 'CA', 'MX'] if args.region == 'ALL' else [args.region]
    tile_codes = []
    for region in regions:
        tile_codes.extend(SRTM_TILES.get(region, []))

    logger.info(f"SRTM Downloader - USGS API")
    logger.info(f"Region: {args.region}")
    logger.info(f"Tiles: {len(tile_codes)}")
    logger.info(f"Output: {output_dir}")
    logger.info("")

    # Get API token
    try:
        api_token = get_api_token()
    except ValueError as e:
        logger.error(str(e))
        return 1

    # Authenticate
    session_token = login(api_token)
    if not session_token:
        logger.error("Failed to authenticate")
        return 1

    # Search for tiles
    logger.info(f"Searching for {len(tile_codes)} SRTM tiles...")
    results = search_srtm_tiles(session_token, tile_codes)

    if not results:
        logger.warning("No tiles found. USGS API may need different parameters.")
        logger.info("Alternative: Download manually from https://earthexplorer.usgs.gov/")
        return 0

    logger.info(f"Found {len(results)} tiles available for download")
    logger.info("")
    logger.info("Downloading tiles...")

    # Download with thread pool
    downloaded = 0
    failed = 0

    with ThreadPoolExecutor(max_workers=3) as executor:
        futures = {
            executor.submit(download_tile, result['entityId'], session_token, output_dir): result
            for result in results
        }

        for future in as_completed(futures):
            try:
                if future.result():
                    downloaded += 1
                else:
                    failed += 1
            except Exception as e:
                logger.error(f"Download failed: {e}")
                failed += 1

    logger.info("")
    logger.info(f"Download Summary:")
    logger.info(f"  Downloaded: {downloaded} tiles")
    logger.info(f"  Failed: {failed} tiles")
    logger.info("")

    if downloaded > 0:
        logger.info("Next steps:")
        logger.info(f"  1. Merge tiles: cd {output_dir} && gdalbuildvrt combined.vrt *.tif")
        logger.info(f"  2. Load: python3 load_elevation_simple.py --geotiff {output_dir}/combined.vrt")
        return 0
    else:
        logger.error("No tiles downloaded")
        return 1

if __name__ == '__main__':
    import sys
    sys.exit(main())
