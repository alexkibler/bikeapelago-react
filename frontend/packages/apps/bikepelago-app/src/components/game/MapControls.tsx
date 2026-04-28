import { useEffect } from 'react';

import L from 'leaflet';
import { useMap, useMapEvents } from 'react-leaflet';

import { useGameStore } from '../../store/gameStore';
import type { MapNode } from '../../types/game';
import ConfirmDialog from '../layout/ConfirmDialog';

// Map fit bounds constants
const FIT_BOUNDS = {
  PADDING: [48, 48] as [number, number],
  MAX_ZOOM: 15,
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
    const latlngs = nodes.map((n) => L.latLng(n.lat, n.lon));
    const bounds = L.latLngBounds(latlngs);
    if (bounds.isValid()) {
      map.fitBounds(bounds, {
        padding: FIT_BOUNDS.PADDING,
        maxZoom: FIT_BOUNDS.MAX_ZOOM,
      });
    }
  }, [map, nodes]);

  return null;
};

// Map click listener — sets the custom route origin pin.
// Clicking the map always moves the pin (never toggles it off); click the pin marker to remove it.
// When an active route exists, the click is held in pendingMapClick until the user confirms.
// It includes a "smart" check to ignore clicks near existing nodes to prevent accidental pin drops.
export const MapEvents = ({ nodes }: { nodes: MapNode[] }) => {
  const map = useMap();
  const setCustomOrigin = useGameStore((s) => s.setCustomOrigin);
  const setPendingMapClick = useGameStore((s) => s.setPendingMapClick);
  const routeData = useGameStore((s) => s.routeData);

  useMapEvents({
    click(e) {
      // Smart check: ignore clicks near nodes (28px radius)
      const clickPoint = map.latLngToLayerPoint(e.latlng);
      const isNearNode = nodes.some((node) => {
        const nodePoint = map.latLngToLayerPoint([node.lat, node.lon]);
        return clickPoint.distanceTo(nodePoint) < 28;
      });

      if (isNearNode) return;

      if (routeData.polyline.length > 0) {
        setPendingMapClick([e.latlng.lat, e.latlng.lng]);
      } else {
        setCustomOrigin([e.latlng.lat, e.latlng.lng]);
      }
    },
  });
  return null;
};

// Renders a confirmation dialog when the user clicks the map with an active route.
// Must be rendered outside the MapContainer so it sits above the map.
export const MapClickConfirmDialog = () => {
  const pendingMapClick = useGameStore((s) => s.pendingMapClick);
  const setPendingMapClick = useGameStore((s) => s.setPendingMapClick);
  const setCustomOrigin = useGameStore((s) => s.setCustomOrigin);

  if (!pendingMapClick) return null;

  return (
    <ConfirmDialog
      title='Reset Route?'
      message='Changing the start point will reset your current route.'
      confirmLabel='Continue'
      onConfirm={() => {
        setCustomOrigin(pendingMapClick);
        setPendingMapClick(null);
      }}
      onCancel={() => setPendingMapClick(null)}
    />
  );
};
