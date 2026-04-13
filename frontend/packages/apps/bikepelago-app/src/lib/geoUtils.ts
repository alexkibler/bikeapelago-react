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

export function generateGPXFromCoordinates(coordinates: [number, number, number?][]): string {
  let gpx = `<?xml version="1.0" encoding="UTF-8"?>
<gpx version="1.1" creator="Bikeapelago" xmlns="http://www.topografix.com/GPX/1/1">
  <trk>
    <name>Bikeapelago Route</name>
    <trkseg>`;
  
  coordinates.forEach((coord) => {
    gpx += `
      <trkpt lat="${coord[0]}" lon="${coord[1]}">
        <ele>${coord[2] || 0}</ele>
      </trkpt>`;
  });
  
  gpx += `
    </trkseg>
  </trk>
</gpx>`;

  return gpx;
}

export function downloadGPXFromPolyline(coordinates: [number, number, number?][]) {
  const gpx = generateGPXFromCoordinates(coordinates);

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
