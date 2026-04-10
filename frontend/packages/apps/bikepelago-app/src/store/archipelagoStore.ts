import { create } from 'zustand';

export type ArchipelagoStatus = 'disconnected' | 'connecting' | 'connected' | 'error';

export interface ChatMessage {
  id: string;
  text: string;
  type: 'system' | 'player' | 'item' | 'error';
  timestamp: number;
}

interface ArchipelagoState {
  status: ArchipelagoStatus;
  error: string | null;
  checkedLocationIds: number[];
  receivedItems: { id: number; name: string }[];
  messages: ChatMessage[];
  
  setStatus: (status: ArchipelagoStatus) => void;
  setError: (error: string | null) => void;
  addCheckedLocation: (locationId: number) => void;
  setCheckedLocations: (locationIds: number[]) => void;
  setReceivedItems: (items: { id: number; name: string }[]) => void;
  addMessage: (message: Omit<ChatMessage, 'id' | 'timestamp'>) => void;
  clearMessages: () => void;
}

export const useArchipelagoStore = create<ArchipelagoState>((set) => ({
  status: 'disconnected',
  error: null,
  checkedLocationIds: [],
  receivedItems: [],
  messages: [],
  
  setStatus: (status) => set({ status }),
  setError: (error) => set({ error }),
  addCheckedLocation: (locationId) => set((state) => ({ 
    checkedLocationIds: state.checkedLocationIds.includes(locationId) 
      ? state.checkedLocationIds 
      : [...state.checkedLocationIds, locationId] 
  })),
  setCheckedLocations: (locationIds) => set({ checkedLocationIds: locationIds }),
  setReceivedItems: (items) => set({ receivedItems: items }),
  addMessage: (msg) => set((state) => ({ 
    messages: [...state.messages.slice(-99), { 
      ...msg, 
      id: Math.random().toString(36).substr(2, 9),
      timestamp: Date.now() 
    }] 
  })),
  clearMessages: () => set({ messages: [] })
}));
