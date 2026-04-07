import { create } from 'zustand';

export type GamePanel = 'chat' | 'upload' | 'route' | null;

interface GameState {
  activePanel: GamePanel;
  setActivePanel: (panel: GamePanel) => void;
  togglePanel: (panel: GamePanel) => void;
  
  waypoints: [number, number][];
  addWaypoint: (point: [number, number]) => void;
  clearWaypoints: () => void;
  
  routeData: {
    distance: number;
    elevation: number;
    polyline: string | null;
  };
  setRouteData: (data: { distance: number, elevation: number, polyline: string | null }) => void;
  
  analysisResult: import('../hooks/useFitAnalyzer').FitAnalysisResult | null;
  setAnalysisResult: (result: import('../hooks/useFitAnalyzer').FitAnalysisResult | null) => void;
}

export const useGameStore = create<GameState>((set) => ({
  activePanel: null,
  setActivePanel: (panel) => set({ activePanel: panel }),
  togglePanel: (panel) => set((state) => ({ 
    activePanel: state.activePanel === panel ? null : panel 
  })),
  
  waypoints: [],
  addWaypoint: (point) => set((state) => ({ 
    waypoints: [...state.waypoints, point] 
  })),
  clearWaypoints: () => set({ waypoints: [], routeData: { distance: 0, elevation: 0, polyline: null } }),
  
  routeData: {
    distance: 0,
    elevation: 0,
    polyline: null
  },
  setRouteData: (data) => set({ routeData: data }),
  
  analysisResult: null,
  setAnalysisResult: (result) => set({ analysisResult: result })
}));
