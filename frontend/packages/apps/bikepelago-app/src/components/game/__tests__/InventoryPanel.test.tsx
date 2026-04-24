import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import InventoryPanel from '../InventoryPanel';

vi.mock('react-router-dom', () => ({
  useParams: () => ({ id: 'session-123' }),
}));

vi.mock('../../../hooks/useToast', () => ({
  useToast: () => ({ error: vi.fn(), success: vi.fn() }),
}));

// Mutable state so each test can configure it
const debugState = { debugMode: false };
vi.mock('../../../store/debugStore', () => ({
  useDebugStore: (selector: (s: typeof debugState) => unknown) => selector(debugState),
}));

const gameState = {
  session: { id: 'session-123' } as unknown,
  selectedNodeIds: new Set<string>(),
  triggerSync: vi.fn(),
  clearSelectedNodes: vi.fn(),
};
vi.mock('../../../store/gameStore', () => ({
  useGameStore: (selector?: (s: typeof gameState) => unknown) =>
    selector ? selector(gameState) : gameState,
}));

const archipelagoState = { receivedItems: [] as { id: number; name: string }[] };
vi.mock('../../../store/archipelagoStore', () => ({
  useArchipelagoStore: (selector?: (s: typeof archipelagoState) => unknown) =>
    selector ? selector(archipelagoState) : archipelagoState,
}));

beforeEach(() => {
  debugState.debugMode = false;
  gameState.selectedNodeIds = new Set<string>();
  archipelagoState.receivedItems = [];
});

describe('InventoryPanel', () => {
  it('renders the Inventory heading', () => {
    render(<InventoryPanel />);
    expect(screen.getByText('Inventory')).toBeInTheDocument();
  });

  it('shows all three usable item sections', () => {
    render(<InventoryPanel />);
    expect(screen.getByText('The Detour')).toBeInTheDocument();
    expect(screen.getByText('The Drone')).toBeInTheDocument();
    expect(screen.getByText('Signal Amplifier')).toBeInTheDocument();
  });

  it('shows item counts correctly for received items', () => {
    archipelagoState.receivedItems = [
      { id: 802010, name: 'Detour' },
      { id: 802010, name: 'Detour' },
      { id: 802011, name: 'Drone' },
    ];
    render(<InventoryPanel />);
    // Detour count = 2, Drone count = 1
    expect(screen.getByText('2')).toBeInTheDocument();
    expect(screen.getByText('1')).toBeInTheDocument();
  });

  it('shows "Select a node on map" when player has a Detour but no node selected', () => {
    archipelagoState.receivedItems = [{ id: 802010, name: 'Detour' }];
    render(<InventoryPanel />);
    expect(screen.getAllByText('Select a node on map').length).toBeGreaterThanOrEqual(1);
  });

  it('shows "Use on Selected Node" when a node is selected and player has a Detour', () => {
    archipelagoState.receivedItems = [{ id: 802010, name: 'Detour' }];
    gameState.selectedNodeIds = new Set(['node-1']);
    render(<InventoryPanel />);
    expect(screen.getByText('Use on Selected Node')).toBeInTheDocument();
  });

  it('shows debug inputs when debug mode is on', () => {
    debugState.debugMode = true;
    archipelagoState.receivedItems = [{ id: 802010, name: 'Detour' }];
    render(<InventoryPanel />);
    expect(screen.getByText('Debug Progression')).toBeInTheDocument();
  });
});
