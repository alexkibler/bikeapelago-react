import {
  useApiGetFactory,
  useQuery,
} from '@bikeapelago/shared-data-fetching';
import type { MapNode } from '../../types/game';

export function useSessionNodesGet({ sessionId }: { sessionId: string }) {
  const sessionNodesGetRequest = useApiGetFactory<'/api/sessions/:sessionId/nodes', MapNode[]>('/api/sessions/:sessionId/nodes')

  return useQuery({
    queryKey: ['sessions', sessionId, 'nodes'],
    queryFn: async ({ signal }) => await sessionNodesGetRequest({
      pathParams: {
        sessionId,
      },
      signal
    }),
  })
}

