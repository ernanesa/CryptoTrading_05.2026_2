import { create } from 'zustand';

export type AppMode = 'Offline' | 'Simulation' | 'Paper' | 'Testnet Dry-run' | 'Testnet Real';

interface AppState {
  mode: AppMode;
  setMode: (mode: AppMode) => void;
  isConnected: boolean;
  setIsConnected: (connected: boolean) => void;
}

export const useAppStore = create<AppState>((set) => ({
  mode: 'Offline',
  setMode: (mode) => set({ mode }),
  isConnected: false,
  setIsConnected: (isConnected) => set({ isConnected })
}));
