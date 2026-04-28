import { fireEvent, render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import type { MapNode } from '../../../types/game';
import DebugBanner from '../DebugBanner';

vi.mock('../../../store/authStore', () => ({
  getToken: () => null,
}));

const mockToggle = vi.fn();
const debugState = { debugMode: false, toggle: mockToggle };
vi.mock('../../../store/debugStore', () => ({
  useDebugStore: (selector?: (s: typeof debugState) => unknown) =>
    selector ? selector(debugState) : debugState,
}));

const mockSetNodes = vi.fn();
const mockTriggerSync = vi.fn();
const gameState = {
  nodes: [] as MapNode[],
  session: null as unknown,
  setNodes: mockSetNodes,
  triggerSync: mockTriggerSync,
};
vi.mock('../../../store/gameStore', () => ({
  useGameStore: (selector?: (s: typeof gameState) => unknown) =>
    selector ? selector(gameState) : gameState,
}));

const availableNode = (id: string): MapNode => ({
  id,
  name: `Node ${id}`,
  lat: 40,
  lon: -70,
  state: 'Available',
  is_arrival_checked: false,
  is_precision_checked: false,
  ap_arrival_location_id: 1,
  ap_precision_location_id: 2,
  region_tag: 'Hub',
  has_been_relocated: false,
});

beforeEach(() => {
  debugState.debugMode = false;
  gameState.nodes = [];
  gameState.session = null;
  mockToggle.mockClear();
  mockSetNodes.mockClear();
  mockTriggerSync.mockClear();
});

describe('DebugBanner', () => {
  it('renders nothing when debug mode is off', () => {
    const { container } = render(<DebugBanner />);
    expect(container.firstChild).toBeNull();
  });

  it('shows banner and available node count when debug mode is on', () => {
    debugState.debugMode = true;
    gameState.nodes = [availableNode('1'), availableNode('2')];
    gameState.session = { id: 'session-1' };
    render(<DebugBanner />);
    expect(screen.getByText(/Debug Mode/i)).toBeInTheDocument();
    expect(screen.getByText(/Clear All \(2\)/i)).toBeInTheDocument();
  });

  it('disables "Clear All" when there are no available nodes', () => {
    debugState.debugMode = true;
    gameState.nodes = [{ ...availableNode('1'), state: 'Checked' }];
    gameState.session = { id: 'session-1' };
    render(<DebugBanner />);
    const clearBtn = screen.getByRole('button', { name: /Clear All \(0\)/i });
    expect(clearBtn).toBeDisabled();
  });

  it('calls toggle when Disable button is clicked', () => {
    debugState.debugMode = true;
    gameState.session = { id: 'session-1' };
    render(<DebugBanner />);
    fireEvent.click(screen.getByText('Disable'));
    expect(mockToggle).toHaveBeenCalledOnce();
  });
});
