import { create } from 'zustand';

interface AppState {
  theme: 'light' | 'dark' | 'system';
  setTheme: (theme: 'light' | 'dark' | 'system') => void;
  lastRunId: string | null;
  setLastRunId: (runId: string | null) => void;
  events: any[];
  addEvent: (event: any) => void;
  clearEvents: () => void;
}

export const useAppStore = create<AppState>((set) => ({
  theme: 'system',
  setTheme: (theme) => set({ theme }),
  lastRunId: null,
  setLastRunId: (runId) => set({ lastRunId: runId }),
  events: [],
  addEvent: (event) => set((state) => ({ 
    events: [...state.events.slice(-99), event] // Keep last 100 events
  })),
  clearEvents: () => set({ events: [] }),
}));
