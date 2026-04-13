import { useApiGetFactory, useQuery } from '@bikeapelago/shared-data-fetching';
import type {
  GameSession
} from '../../types/apiResponses';

export function useSessionsGet() {
  const sessionsGetRequest = useApiGetFactory<'/api/sessions', void, GameSession[]>('/api/sessions')

  return useQuery({
    queryKey: ['sessions'],
    queryFn: async () => await sessionsGetRequest({}),
  })
}
