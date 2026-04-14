import { create } from 'zustand';
import type { MapNode, FitAnalysisResult } from '../types/game';
import { apiFetch, ENDPOINTS } from '../lib/api';

export type GamePanel = 'chat' | 'upload' | 'route' | 'inventory' | null;

export type PolylinePoint = [number, number];

interface RouteData {
  distance: number;
  elevation: number;
  polyline: PolylinePoint[];
  gpxString?: string;
}

interface DiscoveryRouteResponse {
  distanceMeters: number;
  elevation?: number;
  geometry: [number, number][];
}

interface SnappedLocation {
  lon: number;
  lat: number;
}

interface OptimizationResponse {
  success: boolean;
  message?: string;
  totalDistanceMeters: number;
  elevation?: number;
  geometry: [number, number][];
  orderedNodeIds: string[];
  gpxString?: string;
  snappedNodeLocations?: Record<string, SnappedLocation>;
}

interface GameState {
  activePanel: GamePanel;
  setActivePanel: (panel: GamePanel) => void;
  togglePanel: (panel: GamePanel) => void;

  waypoints: [number, number][];
  addWaypoint: (point: [number, number]) => void;
  addWaypoints: (points: [number, number][]) => void;
  setWaypoints: (points: [number, number][]) => void;
  clearWaypoints: () => void;

  nodes: MapNode[];
  setNodes: (nodes: MapNode[]) => void;

  routeData: RouteData;
  setRouteData: (data: RouteData) => void;

  isRouting: boolean;
  routingError: string | null;
  fetchRoute: () => Promise<void>;
  optimizeRouteToAvailable: (
    sessionId: string,
    turnByTurn?: boolean,
  ) => Promise<void>;

  analysisResult: FitAnalysisResult | null;
  setAnalysisResult: (result: FitAnalysisResult | null) => void;

  userLocation: [number, number] | null;
  setUserLocation: (point: [number, number] | null) => void;

  syncVersion: number;
  triggerSync: () => void;
}

export const useGameStore = create<GameState>((set, get) => ({
  activePanel: null,
  setActivePanel: (panel) => set({ activePanel: panel }),
  togglePanel: (panel) =>
    set((state) => ({
      activePanel: state.activePanel === panel ? null : panel,
    })),

  waypoints: [],
  addWaypoint: (point) =>
    set((state) => ({
      waypoints: [...state.waypoints, point],
    })),
  addWaypoints: (points) =>
    set((state) => ({
      waypoints: [...state.waypoints, ...points],
    })),
  setWaypoints: (waypoints) => set({ waypoints }),
  clearWaypoints: () =>
    set({
      waypoints: [],
      routeData: { distance: 0, elevation: 0, polyline: [] },
      routingError: null,
    }),

  nodes: [],
  setNodes: (nodes) => set({ nodes }),

  routeData: {
    distance: 0,
    elevation: 0,
    polyline: [],
  },
  setRouteData: (data) => set({ routeData: data }),

  isRouting: false,
  routingError: null,
  fetchRoute: async () => {
    const { waypoints } = get();
    if (waypoints.length < 2) {
      set({
        routeData: { distance: 0, elevation: 0, polyline: [] },
        routingError: null,
      });
      return;
    }

    set({ isRouting: true, routingError: null });
    try {
      const data = await apiFetch<DiscoveryRouteResponse>(
        ENDPOINTS.DISCOVERY.ROUTE,
        {
          method: 'POST',
          body: JSON.stringify({
            waypoints: waypoints.map((wp) => ({ lat: wp[0], lon: wp[1] })),
            profile: 'cycling',
          }),
        },
      );

      // Parse geometry safely. Expecting array of [lon, lat] from Mapbox geojson
      const polyline = (data.geometry || []).map(
        (p: [number, number]) => [p[1], p[0]] as PolylinePoint,
      );

      set({
        routeData: {
          distance: data.distanceMeters / 1000,
          elevation: data.elevation || 0,
          polyline,
        },
        isRouting: false,
      });
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Routing failed';
      set({ routingError: message, isRouting: false });
    }
  },

  optimizeRouteToAvailable: async (sessionId: string, turnByTurn = true) => {
    const { nodes, userLocation } = get();
    set({ isRouting: true, routingError: null });

    try {
      const data = await apiFetch<OptimizationResponse>(
        `${ENDPOINTS.SESSIONS}/${sessionId}/route-to-available?profile=cycling&turnByTurn=${turnByTurn}`,
        {
          method: 'POST',
        },
      );

      if (data.success) {
        const polyline = (data.geometry || []).map(
          (p: [number, number]) => [p[1], p[0]] as PolylinePoint,
        );

        set({
          routeData: {
            distance: data.totalDistanceMeters / 1000,
            elevation: data.elevation || 0,
            polyline,
            gpxString: data.gpxString,
          },
        });

        const allNodesMap = new Map(nodes.map((n) => [n.id, n]));
        const orderedPoints: [number, number][] = data.orderedNodeIds
          .map((id: string) => {
            const snapped = data.snappedNodeLocations?.[id];
            if (snapped) return [snapped.lat, snapped.lon] as [number, number];
            const node = allNodesMap.get(id);
            return node ? ([node.lat, node.lon] as [number, number]) : null;
          })
          .filter((p): p is [number, number] => p !== null);

        const newWaypoints: [number, number][] = userLocation
          ? [userLocation, ...orderedPoints]
          : orderedPoints;

        // Apply snapped positions to the in-memory node list so markers
        // render on the route without a refetch
        let updatedNodes = nodes;
        if (data.snappedNodeLocations) {
          updatedNodes = nodes.map((n) => {
            const snapped = data.snappedNodeLocations![n.id];
            return snapped ? { ...n, lat: snapped.lat, lon: snapped.lon } : n;
          });
        }

        set({ waypoints: newWaypoints, nodes: updatedNodes, isRouting: false });
      } else {
        throw new Error(data.message || 'Optimization failed');
      }
    } catch (err: unknown) {
      const message =
        err instanceof Error ? err.message : 'Optimization failed';
      set({ routingError: message, isRouting: false });
    }
  },

  analysisResult: null,
  setAnalysisResult: (result) => set({ analysisResult: result }),

  userLocation: null,
  setUserLocation: (location) => set({ userLocation: location }),

  syncVersion: 0,
  triggerSync: () => set((state) => ({ syncVersion: state.syncVersion + 1 })),
}));
