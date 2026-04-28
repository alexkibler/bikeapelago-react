import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';

import Toggle from '../Toggle';

describe('Toggle', () => {
  it('toggles and focuses the switch when its label is clicked', () => {
    const onCheckedChange = vi.fn();

    render(
      <Toggle
        label='Debug mode'
        checked={false}
        onCheckedChange={onCheckedChange}
      />,
    );

    const label = screen.getByText('Debug mode');
    const switchControl = screen.getByRole('switch', { name: 'Debug mode' });

    fireEvent.click(label);

    expect(onCheckedChange).toHaveBeenCalledExactlyOnceWith(true);
    expect(switchControl).toHaveFocus();
  });

  it('generates a fallback id so labeled toggles are accessible without caller-provided ids', () => {
    render(
      <Toggle label='Show routes' checked={false} onCheckedChange={vi.fn()} />,
    );

    const switchControl = screen.getByRole('switch', { name: 'Show routes' });
    expect(switchControl).toHaveAttribute('id');
    expect(switchControl.id).not.toBe('');
  });
});
