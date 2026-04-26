import { getToken, handleUnauthorized } from '../store/authStore';

const API_ORIGIN =
  (import.meta.env.VITE_PUBLIC_API_URL as string | undefined) ?? '';
export const API_BASE = `${API_ORIGIN}/api`;

export const ENDPOINTS = {
  SESSIONS: `${API_BASE}/sessions`,
  AUTH: {
    LOGIN: `${API_BASE}/auth/login`,
    REGISTER: `${API_BASE}/auth/register`,
    ME: `${API_BASE}/auth/me`,
  },
  ITEMS: {
    DETOUR: (sessionId: string) =>
      `${API_BASE}/sessions/${sessionId}/items/detour`,
    DRONE: (sessionId: string) =>
      `${API_BASE}/sessions/${sessionId}/items/drone`,
    SIGNAL_AMPLIFIER: (sessionId: string) =>
      `${API_BASE}/sessions/${sessionId}/items/signal-amplifier`,
    DEBUG_SET_ITEM_COUNT: (sessionId: string) =>
      `${API_BASE}/sessions/${sessionId}/debug/items`,
  },
};

export async function apiFetch<T>(
  endpoint: string,
  options: RequestInit = {},
): Promise<T> {
  const token = getToken();

  const headers = new Headers(options.headers);
  headers.set('Content-Type', 'application/json');
  if (token) {
    headers.set('Authorization', `Bearer ${token}`);
  }

  const response = await fetch(endpoint, {
    ...options,
    headers,
  });

  if (response.status === 401) {
    handleUnauthorized();
    throw new Error('Unauthorized');
  }

  if (!response.ok) {
    let message = `API request failed with status ${response.status}`;
    try {
      const errorData = (await response.json()) as { message?: unknown };
      message =
        typeof errorData.message === 'string' ? errorData.message : message;
    } catch {
      // If JSON parsing fails, we use the default status message
    }
    throw new Error(message);
  }

  try {
    return (await response.json()) as Promise<T>;
  } catch {
    throw new Error('Failed to parse server response');
  }
}
