#!/usr/bin/env python3
"""
Download SRTM tiles for Pennsylvania only (test area).
Pennsylvania tiles: N40W080, N40W075
"""

import urllib.request
import tarfile
from pathlib import Path
import logging

logging.basicConfig(level=logging.INFO, format='%(levelname)s: %(message)s')
logger = logging.getLogger(__name__)

# SRTM tiles for Pennsylvania
PA_TILES = [
    'N40W080',  # Central/Western PA
    'N40W075',  # Eastern PA
]

# Mirror that doesn't require authentication
# Using OpenTopography's public mirror
SRTM_URLS = {
    'N40W080': 'https://cloud.sdsc.edu/v1/AUTH_opentopography/Raster/SRTM_GL30/SRTM_GL30_srtm/N40W080_srtm.tar.gz',
    'N40W075': 'https://cloud.sdsc.edu/v1/AUTH_opentopography/Raster/SRTM_GL30/SRTM_GL30_srtm/N40W075_srtm.tar.gz',
}

def download_and_extract(tile_code: str, output_dir: Path) -> Path | None:
    """Download and extract a single SRTM tile."""
    output_dir.mkdir(parents=True, exist_ok=True)

    url = SRTM_URLS.get(tile_code)
    if not url:
        logger.error(f"No URL for {tile_code}")
        return None

    tar_file = output_dir / f"{tile_code}.tar.gz"

    try:
        logger.info(f"Downloading {tile_code} from OpenTopography...")
        logger.info(f"  URL: {url}")

        urllib.request.urlretrieve(url, tar_file)
        logger.info(f"✓ Downloaded {tar_file.name}")

        # Extract
        logger.info(f"Extracting {tile_code}...")
        with tarfile.open(tar_file) as tar:
            members = tar.getmembers()
            tif_member = next((m for m in members if m.name.endswith('.tif')), None)
            if tif_member:
                tar.extract(tif_member, output_dir)
                tif_path = output_dir / tif_member.name
                logger.info(f"✓ Extracted to {tif_path}")
                tar_file.unlink()  # Clean up tar
                return tif_path
    except Exception as e:
        logger.error(f"Failed to download {tile_code}: {e}")
        if tar_file.exists():
            tar_file.unlink()

    return None

def main():
    output_dir = Path.home() / 'Downloads' / 'srtm_pa'

    logger.info(f"Downloading SRTM tiles for Pennsylvania to {output_dir}")
    logger.info(f"Tiles: {', '.join(PA_TILES)}")

    geotiffs = []
    for tile in PA_TILES:
        tif = download_and_extract(tile, output_dir)
        if tif:
            geotiffs.append(tif)

    if geotiffs:
        logger.info(f"\n✅ Downloaded {len(geotiffs)} tiles:")
        for tif in geotiffs:
            logger.info(f"  - {tif}")
        logger.info(f"\nNext step: Merge with gdalbuildvrt")
        logger.info(f"  cd {output_dir}")
        logger.info(f"  gdalbuildvrt combined.vrt *.tif")
    else:
        logger.error("No tiles downloaded")
        return 1

    return 0

if __name__ == '__main__':
    import sys
    sys.exit(main())
