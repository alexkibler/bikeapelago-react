import { create } from 'zustand';

interface FitImportState {
  pendingFile: File | null;
  setPendingFile: (file: File | null) => void;
}

export const useFitImportStore = create<FitImportState>((set) => ({
  pendingFile: null,
  setPendingFile: (file) => set({ pendingFile: file }),
}));
