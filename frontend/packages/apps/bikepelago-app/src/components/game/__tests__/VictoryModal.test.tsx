import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import VictoryModal from '../VictoryModal';
import type { GameSession } from '../../../types/game';

const mockNavigate = vi.fn();

vi.mock('react-router-dom', () => ({
  useNavigate: () => mockNavigate,
}));

vi.mock('canvas-confetti', () => ({
  default: vi.fn(),
}));

const mockSession: GameSession = {
  id: 'session-1',
  name: 'Test Run',
  ap_slot_name: 'Player One',
  connection_mode: 'singleplayer',
  transport_mode: 'bike',
  progression_mode: 'None',
  north_pass_received: false,
  east_pass_received: false,
  south_pass_received: false,
  west_pass_received: false,
  radius_step: 0,
  macguffins_required: 5,
  macguffins_collected: 5,
  status: 'Completed',
};

describe('VictoryModal', () => {
  it('renders the victory heading', () => {
    render(<VictoryModal session={mockSession} />);
    expect(screen.getByText('You Win!')).toBeInTheDocument();
  });

  it('shows the session name', () => {
    render(<VictoryModal session={mockSession} />);
    expect(screen.getByText(/Test Run/)).toBeInTheDocument();
  });

  it('falls back to ap_seed_name when name is absent', () => {
    const sessionWithSeedName: GameSession = { ...mockSession, name: undefined, ap_seed_name: 'Seed ABC' };
    render(<VictoryModal session={sessionWithSeedName} />);
    expect(screen.getByText(/Seed ABC/)).toBeInTheDocument();
  });

  it('falls back to "Session" when neither name nor ap_seed_name is set', () => {
    const sessionNoName: GameSession = { ...mockSession, name: undefined, ap_seed_name: undefined };
    render(<VictoryModal session={sessionNoName} />);
    expect(screen.getByText(/Session complete/)).toBeInTheDocument();
  });

  it('displays macguffin counts', () => {
    render(<VictoryModal session={mockSession} />);
    expect(screen.getByText('5')).toBeInTheDocument();
    expect(screen.getByText('/ 5')).toBeInTheDocument();
  });

  it('navigates to / when "Back to Sessions" is clicked', () => {
    render(<VictoryModal session={mockSession} />);
    fireEvent.click(screen.getByText('Back to Sessions'));
    expect(mockNavigate).toHaveBeenCalledWith('/');
  });
});
