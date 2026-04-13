import { useEffect } from 'react';
import { useMap, useMapEvents } from 'react-leaflet';
import L from 'leaflet';
import { useGameStore } from '../../store/gameStore';
import type { MapNode } from '../../types/game';

// Map fit bounds constants
const FIT_BOUNDS = {
  PADDING: [48, 48] as [number, number],
  MAX_ZOOM: 15
} as const;

// Map resizer to handle container boundary updates when Layout triggers changes
export const MapResizer = () => {
  const map = useMap();
  useEffect(() => {
    const observer = new ResizeObserver(() => {
      map.invalidateSize();
    });
    const container = map.getContainer();
    observer.observe(container);
    return () => observer.disconnect();
  }, [map]);
  return null;
};

// Auto-fits map to the bounding box of all nodes whenever they change
export const MapAutoFitter = ({ nodes }: { nodes: MapNode[] }) => {
  const map = useMap();

  useEffect(() => {
    if (nodes.length === 0) return;
    const latlngs = nodes.map(n => L.latLng(n.lat, n.lon));
    const bounds = L.latLngBounds(latlngs);
    if (bounds.isValid()) {
      map.fitBounds(bounds, { padding: FIT_BOUNDS.PADDING, maxZoom: FIT_BOUNDS.MAX_ZOOM });
    }
  }, [map, nodes]);

  return null;
};

// Map click listener for routing
export const MapEvents = () => {
  const activePanel = useGameStore(s => s.activePanel);
  const addWaypoint = useGameStore(s => s.addWaypoint);
  
  useMapEvents({
    click(e) {
      if (activePanel === 'route') {
        addWaypoint([e.latlng.lat, e.latlng.lng]);
      }
    },
  });
  return null;
};
