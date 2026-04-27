## 2024-06-25 - Expensive JSON Parsing in Render Loop
**Learning:** Found synchronous `JSON.parse` operations running on potentially large spatial data (polylines) directly inside the React component render loop in `GameView.tsx`. This causes the main thread to block and results in laggy UI interactions whenever the component re-renders (e.g., state updates not related to the polyline).
**Action:** Always extract and memoize `JSON.parse` and array `.map()` transformations of large datasets using `useMemo` so they only recalculate when the source data actually changes.

## 2024-08-01 - Avoid React-Leaflet Iteration Bottlenecks
**Learning:** React-Leaflet component instances (like `Marker` and `L.divIcon`) inside map loops can severely block the main thread if they recreate DOM nodes on unrelated state changes (like when a volatile user location updates).
**Action:** Extract expensive icon instantiation (e.g. `L.divIcon`) into an outer cache (`Map`), and always wrap large lists rendered within map canvases inside a `useMemo` block with exact dependencies.
