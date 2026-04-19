import { useDeleteSession, useSessionsGet } from '../operations/sessions';

export function useSessions() {
  const sessionsQuery = useSessionsGet();

  const deleteSessionMutation = useDeleteSession();

  return {
    sessions: sessionsQuery.data,
    loading: sessionsQuery.isLoading,
    error: sessionsQuery.error?.message,
    deleteSession: deleteSessionMutation,
  };
}
