import { useFitImportStore } from '../store/fitImportStore';
import { useGameStore } from '../store/gameStore';

/**
 * Resets all session-scoped store state. Call on logout or unauthorized.
 * archipelagoStore is reset transitively via gameStore.reset().
 */
export function resetStores(): void {
  useGameStore.getState().reset();
  useFitImportStore.getState().setPendingFile(null);
}
