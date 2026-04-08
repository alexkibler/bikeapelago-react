import { create } from 'zustand';

export interface User {
  id: string;
  username: string;
  name: string;
  weight: number;
  avatar: string | null;
  email: string | null;
}

interface AuthState {
  user: User | null;
  token: string | null;
  isValid: boolean;
  login: (token: string, user: User) => void;
  logout: () => void;
  updateUser: (user: User) => void;
  refresh: () => Promise<void>;
  getToken: () => string | null;
  handleUnauthorized: () => void;
}

const STORAGE_TOKEN_KEY = 'auth_token';
const STORAGE_USER_KEY = 'auth_user';

function loadFromStorage(): { user: User | null; token: string | null } {
  try {
    const token = localStorage.getItem(STORAGE_TOKEN_KEY);
    const userJson = localStorage.getItem(STORAGE_USER_KEY);
    if (token && userJson) {
      const user = JSON.parse(userJson) as User;
      return { token, user };
    }
  } catch {
    // ignore parse errors
  }
  return { token: null, user: null };
}

const initial = loadFromStorage();

export const useAuthStore = create<AuthState>((set, get) => ({
  user: initial.user,
  token: initial.token,
  isValid: !!(initial.token && initial.user),

  login: (token, user) => {
    localStorage.setItem(STORAGE_TOKEN_KEY, token);
    localStorage.setItem(STORAGE_USER_KEY, JSON.stringify(user));
    set({ user, token, isValid: true });
  },

  logout: () => {
    localStorage.removeItem(STORAGE_TOKEN_KEY);
    localStorage.removeItem(STORAGE_USER_KEY);
    set({ user: null, token: null, isValid: false });
  },

  updateUser: (user) => {
    localStorage.setItem(STORAGE_USER_KEY, JSON.stringify(user));
    set({ user });
  },

  // JWT is stateless with a 7-day expiry — no server refresh needed
  refresh: async () => {
    // no-op
  },

  getToken: () => get().token,

  handleUnauthorized: () => {
    localStorage.removeItem(STORAGE_TOKEN_KEY);
    localStorage.removeItem(STORAGE_USER_KEY);
    set({ user: null, token: null, isValid: false });
  },
}));

/**
 * Returns the current auth token. Use in hooks and pages that need
 * to attach Authorization headers to fetch calls.
 */
export function getToken(): string | null {
  return useAuthStore.getState().token;
}

/**
 * Call this whenever a fetch returns 401.
 * Clears the auth store so PrivateRoute redirects to /login.
 */
export function handleUnauthorized(): void {
  useAuthStore.getState().handleUnauthorized();
}
