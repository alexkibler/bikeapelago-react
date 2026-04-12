## POST /api/sessions/setup-from-route

Creates a singleplayer session from a GPX or FIT route file with deterministic node progression.

**Authentication:** Required (Bearer token)

**Request:**
- `file` (multipart): GPX or FIT file containing route track points
- `nodeCount` (int): Number of nodes to distribute along the route (min: 2)

**Response:** 200 OK
```json
{
  "session": {
    "id": "uuid",
    "mode": "singleplayer",
    "status": "Active",
    "location": { "type": "Point", "coordinates": [lon, lat] },
    "radius": 5000
  },
  "summary": {
    "nodeCount": 5,
    "centerLat": 40.123,
    "centerLon": -74.456,
    "radius": 5000
  }
}
```

**Behavior:**
- Parses route file and interpolates evenly along the path
- Creates session with first 3 nodes Available, rest Hidden
- As nodes are checked (state→Checked), next hidden node becomes Available sequentially
