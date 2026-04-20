/**
 * Calculate the distance between two points on Earth using the Haversine formula.
 * Returns distance in kilometers.
 */
export function calculateDistance(
  lat1: number,
  lon1: number,
  lat2: number,
  lon2: number,
): number {
  const R = 6371; // Earth radius in km
  const dLat = ((lat2 - lat1) * Math.PI) / 180;
  const dLon = ((lon2 - lon1) * Math.PI) / 180;
  const a =
    Math.sin(dLat / 2) * Math.sin(dLat / 2) +
    Math.cos((lat1 * Math.PI) / 180) *
      Math.cos((lat2 * Math.PI) / 180) *
      Math.sin(dLon / 2) *
      Math.sin(dLon / 2);
  const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
  return R * c;
}

export function downloadGPX(gpx: string, filename = 'bikeapelago_route.gpx') {
  const blob = new Blob([gpx], { type: 'application/gpx+xml' });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = filename;
  document.body.appendChild(a);
  a.click();
  document.body.removeChild(a);
  URL.revokeObjectURL(url);
}

// ⚡ Bolt Optimization: Accept coordinate array directly instead of parsing JSON string.
// This prevents blocking the main thread during serialization/deserialization of large route arrays.
export function downloadGPXFromPolyline(coordinates: [number, number, number?][]) {
  let gpx = `<?xml version="1.0" encoding="UTF-8"?>
<gpx version="1.1" creator="Bikeapelago" xmlns="http://www.topografix.com/GPX/1/1" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xsi:schemaLocation="http://www.topografix.com/GPX/1/1 http://www.topografix.com/GPX/1/1/gpx.xsd">
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

  downloadGPX(gpx);
}

export function generateGPXFromNodes(
  nodes: { name: string; lat: number; lon: number }[],
) {
  let gpx = `<?xml version="1.0" encoding="UTF-8"?>
<gpx version="1.1" creator="Bikeapelago" xmlns="http://www.topografix.com/GPX/1/1" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xsi:schemaLocation="http://www.topografix.com/GPX/1/1 http://www.topografix.com/GPX/1/1/gpx.xsd">
  <rte>
    <name>Bikeapelago Destinations</name>`;

  nodes.forEach((node) => {
    // Basic XML escape for name
    const escapedName = node.name
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&apos;');

    gpx += `
    <rtept lat="${node.lat}" lon="${node.lon}">
      <name>${escapedName}</name>
    </rtept>`;
  });

  gpx += `
  </rte>
</gpx>`;

  return gpx;
}
