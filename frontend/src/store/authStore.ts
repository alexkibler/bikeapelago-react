import { create } from 'zustand';
import PocketBase, { type AuthModel } from 'pocketbase';

// Point PocketBase to the .NET proxy rather than directly to the DB container
const url = import.meta.env.VITE_PUBLIC_API_URL ? `${import.meta.env.VITE_PUBLIC_API_URL}/api/pb` : '/api/pb';

export const pb = new PocketBase(url);
pb.autoCancellation(false);

interface AuthState {
  user: AuthModel | null;
  isValid: boolean;
  login: (token: string, model: AuthModel) => void;
  logout: () => void;
  refresh: () => Promise<void>;
}

export const useAuthStore = create<AuthState>((set) => ({
  user: pb.authStore.model,
  isValid: pb.authStore.isValid,

  login: (token, model) => {
    pb.authStore.save(token, model);
    set({ user: pb.authStore.model, isValid: pb.authStore.isValid });
  },

  logout: () => {
    pb.authStore.clear();
    set({ user: null, isValid: false });
  },

  refresh: async () => {
    if (pb.authStore.isValid) {
      try {
        await pb.collection('users').authRefresh();
        set({ user: pb.authStore.model, isValid: pb.authStore.isValid });
      } catch {
        pb.authStore.clear();
        set({ user: null, isValid: false });
      }
    }
  }
}));

// Set up listener to auto-sync with PocketBase state
if (typeof pb.authStore.onChange === 'function') {
  pb.authStore.onChange((token, model) => {
    useAuthStore.setState({ user: model, isValid: !!token });
  });
}

/**
 * Call this whenever a fetch returns 401.
 * Clears the auth store so PrivateRoute redirects to /login.
 */
export function handleUnauthorized() {
  pb.authStore.clear();
}
