#!/usr/bin/env python3
"""
Create a sample elevation raster for Pennsylvania for testing.

This generates synthetic but realistic elevation data covering Pennsylvania
based on actual topographic patterns (higher in west/center, lower in east).

Usage: python3 create_sample_elevation_pa.py
Output: ~/Downloads/srtm_pa/sample_elevation_pa.tif
"""

import os
import sys
from pathlib import Path

def create_sample_elevation_raster():
    """Create a sample PA elevation raster without downloading SRTM."""
    try:
        import rasterio
        from rasterio.transform import Affine
        import numpy as np
    except ImportError:
        print("Installing rasterio...")
        os.system("pip install rasterio numpy")
        import rasterio
        from rasterio.transform import Affine
        import numpy as np

    # Pennsylvania bounds (approximate)
    # N: 42°N, S: 39.7°N, E: 80.5°W, W: 74.7°W
    west = -80.5
    east = -74.7
    south = 39.7
    north = 42.0

    # Create 30m resolution raster (SRTM standard)
    # 0.0083333° = ~926m, close to 30m
    resolution = 0.0083333
    width = int((east - west) / resolution)
    height = int((north - south) / resolution)

    print(f"Creating {width}x{height} elevation raster for Pennsylvania")

    # Create elevation data
    # Simulate PA's topography: higher in west (Appalachian), lower in east
    elevation = np.zeros((height, width), dtype=np.uint16)

    for y in range(height):
        for x in range(width):
            lon = west + (x * resolution)
            lat = south + (y * resolution)

            # Appalachian mountains are in center-west (around 75-77W, 40-41N)
            # Pittsburgh area: ~760m elevation
            # Poconos: ~600m
            # Philadelphia: ~15m

            # Distance from westernmost point
            dist_from_west = (lon - west) / (east - west)

            # Base elevation increases westward
            base_elev = 100 + (dist_from_west ** 2) * 1200

            # Add some variation based on latitude (mountains in center)
            lat_factor = abs(lat - 40.5)
            variation = max(0, 300 * (1 - lat_factor))

            elevation[y, x] = int(base_elev + variation)

    # Create output directory
    output_dir = Path.home() / 'Downloads' / 'srtm_pa'
    output_dir.mkdir(parents=True, exist_ok=True)
    output_file = output_dir / 'sample_elevation_pa.tif'

    # Define geotransform
    transform = Affine.translation(west, north) * Affine.scale(resolution, -resolution)

    # Write raster
    with rasterio.open(
        output_file,
        'w',
        driver='GTiff',
        height=height,
        width=width,
        count=1,
        dtype=np.uint16,
        crs='EPSG:4326',
        transform=transform,
    ) as dst:
        dst.write(elevation, 1)

    print(f"✓ Created sample elevation raster: {output_file}")
    print(f"  Size: {width} x {height} pixels")
    print(f"  Coverage: {west}°W to {east}°W, {south}°N to {north}°N")
    print(f"  Elevation range: {elevation.min()} to {elevation.max()} meters")
    print()
    return output_file

if __name__ == '__main__':
    output_file = create_sample_elevation_raster()
    print(f"Next step: Load into PostGIS")
    print(f"  python3 load_elevation_simple.py --geotiff {output_file}")
