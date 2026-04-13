/**
 * Calculate the distance between two points on Earth using the Haversine formula.
 * Returns distance in kilometers.
 */
export function calculateDistance(lat1: number, lon1: number, lat2: number, lon2: number): number {
  const R = 6371; // Earth radius in km
  const dLat = (lat2 - lat1) * Math.PI / 180;
  const dLon = (lon2 - lon1) * Math.PI / 180;
  const a = 
    Math.sin(dLat / 2) * Math.sin(dLat / 2) +
    Math.cos(lat1 * Math.PI / 180) * Math.cos(lat2 * Math.PI / 180) * 
    Math.sin(dLon / 2) * Math.sin(dLon / 2);
  const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
  return R * c;
}

/**
 * Generates and downloads a GPX file from an array of coordinates.
 *
 * ⚡ Bolt Performance Optimization:
 * Changed signature to accept the coordinate array directly instead of a JSON string.
 * This avoids a redundant and synchronously blocking JSON.parse() operation on
 * potentially large polyline arrays, improving responsiveness on the main thread.
 */
export function downloadGPXFromPolyline(coordinates: [number, number, number?][]) {
  let gpx = `<?xml version="1.0" encoding="UTF-8"?>
<gpx version="1.1" creator="Bikeapelago" xmlns="http://www.topografix.com/GPX/1/1">
  <trk>
    <name>Bikeapelago Route</name>
    <trkseg>`;
  
  coordinates.forEach((coord) => {
    gpx += `
      <trkpt lat="${coord[1]}" lon="${coord[0]}">
        <ele>${coord[2] || 0}</ele>
      </trkpt>`;
  });
  
  gpx += `
    </trkseg>
  </trk>
</gpx>`;

  const blob = new Blob([gpx], { type: 'application/gpx+xml' });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = 'bikeapelago_route.gpx';
  document.body.appendChild(a);
  a.click();
  document.body.removeChild(a);
  URL.revokeObjectURL(url);
}
