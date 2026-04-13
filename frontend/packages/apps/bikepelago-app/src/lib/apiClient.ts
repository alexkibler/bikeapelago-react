import { getToken } from '../store/authStore';

const getHeaders = () => ({
  'Content-Type': 'application/json',
  ...(getToken() ? { 'Authorization': `Bearer ${getToken()}` } : {})
});

export const apiClient = {
  async get<T>(url: string, signal?: AbortSignal): Promise<T> {
    const res = await fetch(url, { headers: getHeaders(), signal });
    if (!res.ok) throw new Error(`${res.status}: ${res.statusText}`);
    return res.json();
  },

  async patch<T>(url: string, body: unknown, signal?: AbortSignal): Promise<T> {
    const res = await fetch(url, {
      method: 'PATCH',
      headers: getHeaders(),
      body: JSON.stringify(body),
      signal
    });
    if (!res.ok) throw new Error(`${res.status}: ${res.statusText}`);
    return res.json();
  }
};
