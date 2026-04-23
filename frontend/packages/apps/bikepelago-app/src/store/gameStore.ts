import { create } from 'zustand';

import { ENDPOINTS, apiFetch } from '../lib/api';
import type { FitAnalysisResult, MapNode } from '../types/game';

export type GamePanel = 'chat' | 'upload' | 'route' | 'inventory' | null;

export type PolylinePoint = [number, number];

interface RouteData {
  distance: number;
  elevation: number;
  polyline: PolylinePoint[];
  gpxString?: string;
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
  setWaypoints: (points: [number, number][]) => void;
  clearWaypoints: () => void;

  /** Node IDs the user has manually selected to include in the route. */
  selectedNodeIds: Set<string>;
  toggleSelectedNode: (id: string) => void;
  clearSelectedNodes: () => void;

  /**
   * User-clicked starting pin on the map.
   * When set, used as the route origin instead of userLocation / session centre.
   * Cleared by clicking the pin marker or pressing Clear Route.
   */
  customOrigin: [number, number] | null;
  setCustomOrigin: (point: [number, number] | null) => void;

  nodes: MapNode[];
  setNodes: (nodes: MapNode[]) => void;

  routeData: RouteData;
  setRouteData: (data: RouteData) => void;

  isRouting: boolean;
  routingError: string | null;

  /**
   * Build a route to the selected nodes (or all available nodes if none are selected).
   * Uses customOrigin → userLocation → session centre as the starting point.
   */
  buildRoute: (sessionId: string, turnByTurn?: boolean) => Promise<void>;

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

  // ── Waypoints (output-only — populated by buildRoute for numbered markers) ──
  waypoints: [],
  setWaypoints: (waypoints) => set({ waypoints }),
  clearWaypoints: () => {
    set({
      waypoints: [],
      routeData: { distance: 0, elevation: 0, polyline: [] },
      routingError: null,
    });
    get().clearSelectedNodes();
    get().setCustomOrigin(null);
  },

  // ── Node selection ──
  selectedNodeIds: new Set<string>(),
  toggleSelectedNode: (id) =>
    set((state) => {
      const next = new Set(state.selectedNodeIds);
      if (next.has(id)) {
        next.delete(id);
      } else {
        next.add(id);
      }
      return { selectedNodeIds: next };
    }),
  clearSelectedNodes: () => set({ selectedNodeIds: new Set<string>() }),

  // ── Custom origin pin ──
  customOrigin: null,
  setCustomOrigin: (point) => set({ customOrigin: point }),

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

  buildRoute: async (sessionId: string, turnByTurn = true) => {
    const { selectedNodeIds, customOrigin, userLocation, nodes } = get();
    set({ isRouting: true, routingError: null });

    try {
      // Build origin: customOrigin > userLocation > omit (server falls back to session centre)
      const origin: { lat: number; lon: number } | undefined =
        customOrigin
          ? { lat: customOrigin[0], lon: customOrigin[1] }
          : userLocation
            ? { lat: userLocation[0], lon: userLocation[1] }
            : undefined;

      // Build nodeIds: selected subset > empty (server uses all Available)
      const nodeIds = selectedNodeIds.size > 0 ? [...selectedNodeIds] : [];

      const data = await apiFetch<OptimizationResponse>(
        `${ENDPOINTS.SESSIONS}/${sessionId}/route`,
        {
          method: 'POST',
          body: JSON.stringify({
            customOrigin: origin,
            nodeIds,
            profile: 'cycling',
            turnByTurn,
          }),
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

        // Apply snapped positions to in-memory node list so markers render on the route
        let updatedNodes = nodes;
        if (data.snappedNodeLocations) {
          updatedNodes = nodes.map((n) => {
            const snapped = data.snappedNodeLocations![n.id];
            return snapped ? { ...n, lat: snapped.lat, lon: snapped.lon } : n;
          });
        }

        // Populate waypoints with ordered node positions for numbered marker display
        const allNodesMap = new Map(updatedNodes.map((n) => [n.id, n]));
        const orderedPoints: [number, number][] = data.orderedNodeIds
          .map((id: string) => {
            const snapped = data.snappedNodeLocations?.[id];
            if (snapped) return [snapped.lat, snapped.lon] as [number, number];
            const node = allNodesMap.get(id);
            return node ? ([node.lat, node.lon] as [number, number]) : null;
          })
          .filter((p): p is [number, number] => p !== null);

        set({
          waypoints: orderedPoints,
          nodes: updatedNodes,
          isRouting: false,
          // Clear selection after a successful route build
          selectedNodeIds: new Set<string>(),
        });
      } else {
        throw new Error(data.message || 'Routing failed');
      }
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Routing failed';
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
