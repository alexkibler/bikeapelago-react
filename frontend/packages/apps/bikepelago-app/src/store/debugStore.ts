import { create } from 'zustand';

const SESSION_KEY = 'debug_mode';

interface DebugState {
  debugMode: boolean;
  toggle: () => void;
}

export const useDebugStore = create<DebugState>((set, get) => ({
  debugMode: sessionStorage.getItem(SESSION_KEY) === 'true',
  toggle: () => {
    const next = !get().debugMode;
    sessionStorage.setItem(SESSION_KEY, String(next));
    set({ debugMode: next });
  },
}));
