import { describe, it, expect } from 'vitest';
import { calculateDistance, generateGPXFromCoordinates } from '../geoUtils';

describe('geoUtils', () => {
  it('calculateDistance should return 0 for same point', () => {
    expect(calculateDistance(40, -70, 40, -70)).toBe(0);
  });

  it('generateGPXFromCoordinates should format gpx from array of coordinates correctly', () => {
    const coords: [number, number, number?][] = [[40, -70], [41, -71, 100]];
    const gpx = generateGPXFromCoordinates(coords);
    expect(gpx).toContain('<trkpt lat="40" lon="-70">');
    expect(gpx).toContain('<ele>0</ele>');
    expect(gpx).toContain('<trkpt lat="41" lon="-71">');
    expect(gpx).toContain('<ele>100</ele>');
  });
});
