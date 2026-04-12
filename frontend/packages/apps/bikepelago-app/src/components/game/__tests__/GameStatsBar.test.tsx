import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import GameStatsBar from '../GameStatsBar';
import type { GameSession, MapNode } from '../../../types/game';

// Mock the archipelago store
vi.mock('../../store/archipelagoStore', () => ({
  useArchipelagoStore: () => ({
    status: 'connected',
    error: null
  })
}));

describe('GameStatsBar', () => {
  const mockSession: GameSession = {
    id: 'test-id',
    name: 'Test Session',
    ap_slot_name: 'Test Rider',
    ap_server_url: 'localhost:38281'
  };

  const mockNodes: MapNode[] = [
    { id: '1', name: 'Node 1', lat: 40, lon: -70, state: 'Checked' },
    { id: '2', name: 'Node 2', lat: 41, lon: -71, state: 'Available' },
    { id: '3', name: 'Node 3', lat: 42, lon: -72, state: 'Hidden' },
    { id: '4', name: 'Node 4', lat: 43, lon: -73, state: 'Hidden' },
  ];

  it('renders session info and node counts', () => {
    render(<GameStatsBar session={mockSession} nodes={mockNodes} />);

    expect(screen.getByText(/Test Session • Test Rider/)).toBeInTheDocument();
    
    // Counts: Hidden (2), Available (1), Checked (1)
    expect(screen.getByText('2')).toBeInTheDocument(); // Hidden
    expect(screen.getAllByText('1')).toHaveLength(2); // Available and Checked
  });

  it('toggles stats info dropdown on click', () => {
    render(<GameStatsBar session={mockSession} nodes={mockNodes} />);
    
    const statsBtn = screen.getByLabelText('Toggle node statistics');
    
    fireEvent.click(statsBtn);
    
    expect(screen.getByText('Hidden')).toBeInTheDocument();
    expect(screen.getByText('Available')).toBeInTheDocument();
    expect(screen.getByText('Checked')).toBeInTheDocument();
  });
});
