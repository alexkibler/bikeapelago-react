import { getToken, handleUnauthorized } from '../store/authStore';

export const API_BASE = '/api';

export const ENDPOINTS = {
  SESSIONS: `${API_BASE}/sessions`,
  DISCOVERY: {
    ROUTE: `${API_BASE}/discovery/route`,
    NODES: `${API_BASE}/discovery/nodes`,
  },
  AUTH: {
    LOGIN: `${API_BASE}/auth/login`,
    REGISTER: `${API_BASE}/auth/register`,
    ME: `${API_BASE}/auth/me`,
  }
};

export async function apiFetch<T>(
  endpoint: string, 
  options: RequestInit = {}
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
      const errorData = await response.json();
      message = errorData.message || message;
    } catch {
      // If JSON parsing fails, we use the default status message
    }
    throw new Error(message);
  }

  try {
    return await response.json() as Promise<T>;
  } catch {
    throw new Error('Failed to parse server response');
  }
}
