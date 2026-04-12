import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import GameStatsBar from '../GameStatsBar';

// Mock the archipelago store
vi.mock('../../store/archipelagoStore', () => ({
  useArchipelagoStore: () => ({
    status: 'connected',
    error: null
  })
}));

describe('GameStatsBar', () => {
  const mockSession = {
    name: 'Test Session',
    ap_slot_name: 'Test Rider'
  };

  const mockNodes = [
    { id: '1', state: 'Checked' },
    { id: '2', state: 'Available' },
    { id: '3', state: 'Hidden' },
    { id: '4', state: 'Hidden' },
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
    
    // Check that detail labels aren't visible initially
    expect(screen.queryByText('Total Checked')).not.toBeInTheDocument(); // Wait, the labels are just "HIDDEN", "AVAILABLE", "CHECKED" in uppercase

    fireEvent.click(statsBtn);
    
    expect(screen.getByText('Hidden')).toBeInTheDocument();
    expect(screen.getByText('Available')).toBeInTheDocument();
    expect(screen.getByText('Checked')).toBeInTheDocument();
  });
});
