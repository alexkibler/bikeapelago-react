## 2024-06-25 - Expensive JSON Parsing in Render Loop
**Learning:** Found synchronous `JSON.parse` operations running on potentially large spatial data (polylines) directly inside the React component render loop in `GameView.tsx`. This causes the main thread to block and results in laggy UI interactions whenever the component re-renders (e.g., state updates not related to the polyline).
**Action:** Always extract and memoize `JSON.parse` and array `.map()` transformations of large datasets using `useMemo` so they only recalculate when the source data actually changes.
## 2024-04-25 - Expensive Marker Icon Generation in Map Rendering Loop
**Learning:** Found repetitive `L.divIcon` instantiation without caching inside `nodes.map(...)` array mappings for hundreds of map markers in `MapCanvas.tsx`. These objects generate deep HTML strings and parsing logic on every small component re-render (e.g. state updates).
**Action:** Extract expensive leaflet icon constructions (like `L.divIcon`) using `ICON_CACHE` Maps and memoize array `.map()` component generation using `useMemo()` inside react-leaflet map rendering loops to ensure they only recalculate on actual data changes.
