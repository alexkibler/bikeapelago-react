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
    ap_server_url: 'localhost:38281',
    connection_mode: 'singleplayer',
    transport_mode: 'bike',
    progression_mode: 'None',
    north_pass_received: false,
    east_pass_received: false,
    south_pass_received: false,
    west_pass_received: false,
    radius_step: 0,
    macguffins_required: 0,
    macguffins_collected: 0,
    status: 'Active',
  };

  const mockNodes: MapNode[] = [
    { id: '1', name: 'Node 1', lat: 40, lon: -70, state: 'Checked', is_arrival_checked: true, is_precision_checked: true, ap_arrival_location_id: 1, ap_precision_location_id: 2, region_tag: 'Hub', has_been_relocated: false },
    { id: '2', name: 'Node 2', lat: 41, lon: -71, state: 'Available', is_arrival_checked: false, is_precision_checked: false, ap_arrival_location_id: 3, ap_precision_location_id: 4, region_tag: 'Hub', has_been_relocated: false },
    { id: '3', name: 'Node 3', lat: 42, lon: -72, state: 'Hidden', is_arrival_checked: false, is_precision_checked: false, ap_arrival_location_id: 5, ap_precision_location_id: 6, region_tag: 'Hub', has_been_relocated: false },
    { id: '4', name: 'Node 4', lat: 43, lon: -73, state: 'Hidden', is_arrival_checked: false, is_precision_checked: false, ap_arrival_location_id: 7, ap_precision_location_id: 8, region_tag: 'Hub', has_been_relocated: false },
  ];

  it('renders session info and node counts', () => {
    render(<GameStatsBar session={mockSession} nodes={mockNodes} />);

    expect(screen.getByText('Test Session')).toBeInTheDocument();
    expect(screen.getByText('Test Rider')).toBeInTheDocument();
    
    // Counts: Arrival (1), Precision (1), Total (4)
    expect(screen.getAllByText('1')).toHaveLength(2); // Arrival and Precision
    expect(screen.getAllByText('/ 4')).toHaveLength(2);
  });

  it('toggles stats info dropdown on click', () => {
    render(<GameStatsBar session={mockSession} nodes={mockNodes} />);
    
    const statsBtn = screen.getByLabelText('Toggle node statistics');
    
    fireEvent.click(statsBtn);
    
    expect(screen.getByText(/Arrival/)).toBeInTheDocument();
    expect(screen.getByText(/Precision/)).toBeInTheDocument();
    expect(screen.getByText('Total Seed Progress')).toBeInTheDocument();
  });
});
