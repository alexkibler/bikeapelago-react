import { getToken, handleUnauthorized } from '../store/authStore';
import { useSessionsGet } from '../operations/sessions';

export function useSessions() {
  const sessionsQuery = useSessionsGet();

  const deleteSession = async (id: string) => {
    try {
      const token = getToken();
      const res = await fetch(`/api/sessions/${id}`, {
        method: 'DELETE',
        headers: token ? { Authorization: `Bearer ${token}` } : {},
      });
      if (!res.ok) {
        if (res.status === 401) {
          handleUnauthorized();
          return;
        }
        const errText = await res.text();
        throw new Error(`HTTP ${res.status} - ${errText}`);
      }
    } catch (err: unknown) {
      console.error('Failed to delete session:', err);
      throw err;
    }
  };

  const deleteAllSessions = async () => {
    try {
      const token = getToken();
      const res = await fetch('/api/sessions/all', {
        method: 'DELETE',
        headers: token ? { Authorization: `Bearer ${token}` } : {},
      });
      if (!res.ok) {
        if (res.status === 401) {
          handleUnauthorized();
          return;
        }
        const errText = await res.text();
        throw new Error(`HTTP ${res.status} - ${errText}`);
      }
    } catch (err: unknown) {
      console.error('Failed to delete all sessions:', err);
      throw err;
    }
  };

  return {
    sessions: sessionsQuery.data,
    loading: sessionsQuery.isLoading,
    error: sessionsQuery.error?.message,
    deleteSession,
    deleteAllSessions,
  };
}
