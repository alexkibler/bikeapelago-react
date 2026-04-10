import React, { useEffect, useRef } from 'react';
import maplibregl from 'maplibre-gl';
import 'maplibre-gl/dist/maplibre-gl.css';

interface SpatialPreviewProps {
  geoJson: any;
  height?: string;
  interactive?: boolean;
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
        data: geoJson
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
      
      const processCoords = (coords: any) => {
        if (Array.isArray(coords[0])) {
           coords.forEach(processCoords);
        } else if (typeof coords[0] === 'number') {
           bounds.extend(coords as [number, number]);
        }
      };

      if (geoJson.type === 'Feature') {
          processCoords(geoJson.geometry.coordinates);
      } else if (geoJson.type === 'FeatureCollection') {
          geoJson.features.forEach((f: any) => processCoords(f.geometry.coordinates));
      } else if (geoJson.coordinates) {
          processCoords(geoJson.coordinates);
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
