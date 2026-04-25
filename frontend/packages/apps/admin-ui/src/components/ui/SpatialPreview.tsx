import React, { useEffect, useRef } from 'react';
import maplibregl from 'maplibre-gl';
import 'maplibre-gl/dist/maplibre-gl.css';

interface SpatialPreviewProps {
  geoJson: unknown;
  height?: string;
  interactive?: boolean;
}

const isRecord = (value: unknown): value is Record<string, unknown> =>
  typeof value === 'object' && value !== null;

const isCoordinatePair = (value: unknown): value is [number, number] =>
  Array.isArray(value) &&
  value.length >= 2 &&
  typeof value[0] === 'number' &&
  typeof value[1] === 'number';

function processCoords(
  coords: unknown,
  bounds: maplibregl.LngLatBounds,
): void {
  if (!Array.isArray(coords)) return;

  if (isCoordinatePair(coords)) {
    bounds.extend(coords);
    return;
  }

  coords.forEach((child) => processCoords(child, bounds));
}

export const SpatialPreview: React.FC<SpatialPreviewProps> = ({ geoJson, height = '200px', interactive = false }) => {
  const mapContainer = useRef<HTMLDivElement>(null);
  const map = useRef<maplibregl.Map | null>(null);

  useEffect(() => {
    if (!mapContainer.current || !geoJson) return;

    map.current = new maplibregl.Map({
      container: mapContainer.current,
      style: {
        version: 8,
        sources: {
          'osm': {
            type: 'raster',
            tiles: ['https://tile.openstreetmap.org/{z}/{x}/{y}.png'],
            tileSize: 256,
            attribution: '&copy; OpenStreetMap Contributors'
          }
        },
        layers: [
          {
            id: 'osm-layer',
            type: 'raster',
            source: 'osm'
          }
        ]
      },
      center: [0, 0],
      zoom: 1,
      interactive: interactive
    });

    map.current.on('load', () => {
      if (!map.current) return;

      map.current.addSource('spatial-data', {
        type: 'geojson',
        data: geoJson as maplibregl.GeoJSONSourceSpecification['data'],
      });

      // Add layers based on geometry type
      map.current.addLayer({
        id: 'spatial-layer-fill',
        type: 'fill',
        source: 'spatial-data',
        paint: {
          'fill-color': '#6366f1',
          'fill-opacity': 0.3
        },
        filter: ['==', '$type', 'Polygon']
      });

      map.current.addLayer({
        id: 'spatial-layer-line',
        type: 'line',
        source: 'spatial-data',
        layout: {
          'line-join': 'round',
          'line-cap': 'round'
        },
        paint: {
          'line-color': '#6366f1',
          'line-width': 3
        },
        filter: ['==', '$type', 'LineString']
      });

      map.current.addLayer({
        id: 'spatial-layer-point',
        type: 'circle',
        source: 'spatial-data',
        paint: {
          'circle-radius': 6,
          'circle-color': '#6366f1',
          'circle-stroke-width': 2,
          'circle-stroke-color': '#ffffff'
        },
        filter: ['==', '$type', 'Point']
      });

      // Fit bounds
      const bounds = new maplibregl.LngLatBounds();
      
      if (isRecord(geoJson) && geoJson.type === 'Feature' && isRecord(geoJson.geometry)) {
        processCoords(geoJson.geometry.coordinates, bounds);
      } else if (isRecord(geoJson) && geoJson.type === 'FeatureCollection' && Array.isArray(geoJson.features)) {
        geoJson.features.forEach((feature) => {
          if (isRecord(feature) && isRecord(feature.geometry)) {
            processCoords(feature.geometry.coordinates, bounds);
          }
        });
      } else if (isRecord(geoJson) && 'coordinates' in geoJson) {
        processCoords(geoJson.coordinates, bounds);
      }

      if (!bounds.isEmpty()) {
        map.current.fitBounds(bounds, { padding: 40, animate: false });
      }
    });

    return () => {
      map.current?.remove();
    };
  }, [geoJson, interactive]);

  return (
    <div 
      ref={mapContainer} 
      className="rounded-xl border border-zinc-800 overflow-hidden shadow-inner bg-zinc-900"
      style={{ height }} 
    />
  );
};
