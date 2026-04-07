import { useState, useEffect } from 'react';
import { getToken, handleUnauthorized } from '../store/authStore';

export interface GameSession {
  id: string;
  ap_seed_name: string | null;
  ap_server_url: string | null;
  ap_slot_name: string | null;
  status: string;
}

export function useSessions() {
  const [sessions, setSessions] = useState<GameSession[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchSessions = async () => {
      try {
        const token = getToken();
        const res = await fetch('/api/sessions', {
          headers: token ? { 'Authorization': `Bearer ${token}` } : {}
        });
        if (!res.ok) {
            if (res.status === 401) { handleUnauthorized(); return; }
            const errText = await res.text();
            throw new Error(`HTTP ${res.status} - ${errText}`);
        }
        const data = await res.json();
        setSessions(data);
      } catch (err: unknown) {
        setError('Failed to load sessions.');
        console.error(err);
      } finally {
        setLoading(false);
      }
    };
    fetchSessions();
  }, []);

  return { sessions, loading, error };
}
