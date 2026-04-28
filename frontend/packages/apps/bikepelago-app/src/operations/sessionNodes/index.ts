import { useApiGetFactory, useQuery } from '@bikeapelago/shared-data-fetching';

import type { MapNode } from '../../types/game';

export function useSessionNodesGet({
  sessionId,
  syncVersion,
}: {
  sessionId: string;
  syncVersion?: number;
}) {
  const sessionNodesGetRequest = useApiGetFactory<
    '/api/sessions/:sessionId/nodes',
    MapNode[]
  >('/api/sessions/:sessionId/nodes');

  return useQuery({
    queryKey: ['sessions', sessionId, 'nodes', syncVersion],
    queryFn: async ({ signal }) =>
      await sessionNodesGetRequest({
        pathParams: {
          sessionId,
        },
        signal,
      }),
  });
}
