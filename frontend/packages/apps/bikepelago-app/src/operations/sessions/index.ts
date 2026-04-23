import {
  useApiGetFactory,
  useApiDeleteFactory,
  useApiPatchFactory,
  useApiPostFactory,
  useMutation,
  useQuery,
} from '@bikeapelago/shared-data-fetching';
import type {
  GameSession
} from '../../types/game';
import { useClearKeys } from '../util';
import type {
  SessionCreateDataInput,
  SessionUniqueWhere,
  SessionUpdateInput
} from './types';

export function useSessionsGet() {
  const sessionsGetRequest = useApiGetFactory<'/api/sessions', GameSession[]>('/api/sessions')

  return useQuery({
    queryKey: ['sessions'],
    queryFn: async ({ signal }) => await sessionsGetRequest({ signal }),
  })
}

export function useSessionGet({ id, syncVersion }: SessionUniqueWhere & { syncVersion?: number }) {
  const sessionGetRequest = useApiGetFactory<'/api/sessions/:id', GameSession>('/api/sessions/:id')

  return useQuery({
    queryKey: ['sessions', id, syncVersion?.toString() ?? 'syncVersion'],
    queryFn: async ({ signal }) => await sessionGetRequest({
      pathParams: { id },
      signal
    }),
  })
}

export function useSessionUpdate({ id }: SessionUniqueWhere) {
  const updateSessionRequest = useApiPatchFactory<'/api/sessions/:id', SessionUpdateInput, GameSession>('/api/sessions/:id')
  const clearKeys = useClearKeys();

  return useMutation({
    mutationFn: async (body: SessionUpdateInput) => await updateSessionRequest({
      body,
      pathParams: { id },
    }),
    onSuccess: () => {
      clearKeys([['sessions', id]])
    },
  })
}

export function useCreateSession() {
  const createSessionRequest = useApiPostFactory<'/api/sessions', SessionCreateDataInput, GameSession>('/api/sessions')
  const clearKeys = useClearKeys();

  return useMutation({
    mutationFn: async ({ id }: SessionUniqueWhere) => await createSessionRequest({
      body: null,
      pathParams: { id },
    }),
    onSuccess: () => {
      clearKeys([['sessions']])
    },
  })
}


export function useDeleteSession() {
  const deleteSessionRequest = useApiDeleteFactory<'/api/sessions/:id', null, unknown>('/api/sessions/:id')
  const clearKeys = useClearKeys();

  return useMutation({
    mutationFn: async ({ id }: SessionUniqueWhere) => await deleteSessionRequest({
      body: null,
      pathParams: { id },
    }),
    onSuccess: () => {
      clearKeys([['sessions']])
    },
  })
}

export function useDeleteAllSessions() {
  const deleteSessionsRequest = useApiDeleteFactory<'/api/sessions', null, unknown>('/api/sessions')
  const clearKeys = useClearKeys();

  return useMutation({
    mutationFn: async () => await deleteSessionsRequest({
      body: null,
    }),
    onSuccess: () => {
      clearKeys([['sessions']])
    },
  })
}
