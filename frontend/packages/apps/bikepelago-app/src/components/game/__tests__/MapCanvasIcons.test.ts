import { describe, expect, it } from 'vitest';

import {
  getMarkerIcon,
  getUserLocationIcon,
  getWaypointIcon,
} from '../MapCanvas';

describe('MapCanvas icon factories', () => {
  it('reuses marker icons for the same visual state', () => {
    expect(getMarkerIcon('Available', true, false)).toBe(
      getMarkerIcon('Available', true, false),
    );
  });

  it('reuses waypoint and user location icons across renders', () => {
    expect(getWaypointIcon(0)).toBe(getWaypointIcon(0));
    expect(getUserLocationIcon()).toBe(getUserLocationIcon());
  });
});
