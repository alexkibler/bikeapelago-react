## 2024-06-25 - Expensive JSON Parsing in Render Loop
**Learning:** Found synchronous `JSON.parse` operations running on potentially large spatial data (polylines) directly inside the React component render loop in `GameView.tsx`. This causes the main thread to block and results in laggy UI interactions whenever the component re-renders (e.g., state updates not related to the polyline).
**Action:** Always extract and memoize `JSON.parse` and array `.map()` transformations of large datasets using `useMemo` so they only recalculate when the source data actually changes.

## 2024-05-15 - [Cache React-Leaflet Icons]
**Learning:** React-Leaflet Markers use `L.divIcon` objects. Creating a new icon on every render changes the object reference, causing Leaflet DOM thrashing and performance degradation, especially with many nodes on the map.
**Action:** Always memoize, extract to constants, or use a Map cache for Leaflet icon factories so referential equality is preserved between re-renders.
