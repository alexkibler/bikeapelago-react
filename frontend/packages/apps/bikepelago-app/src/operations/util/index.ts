import { useCallback } from 'react';
import {
  useQueryClient,
} from '@bikeapelago/shared-data-fetching';

export function useClearKeys() {
  const queryClient = useQueryClient();

  const clearKeys = useCallback((keysToClear: string[][]) => {
    for (const queryKey of keysToClear) {
      queryClient.removeQueries({ queryKey });
    }
  }, [queryClient]);

  return clearKeys
}
