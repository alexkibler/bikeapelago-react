## 2024-06-25 - Expensive JSON Parsing in Render Loop
**Learning:** Found synchronous `JSON.parse` operations running on potentially large spatial data (polylines) directly inside the React component render loop in `GameView.tsx`. This causes the main thread to block and results in laggy UI interactions whenever the component re-renders (e.g., state updates not related to the polyline).
**Action:** Always extract and memoize `JSON.parse` and array `.map()` transformations of large datasets using `useMemo` so they only recalculate when the source data actually changes.

## 2024-06-25 - React Unnecessary .map() operations in render loop
**Learning:** Found mapping operations applied directly to arrays in JSX such as `nodes.map(...)`, `waypoints.map(...)` and inline generation of `<Marker>` or `<Polyline>` arrays in `MapCanvas.tsx`. These array manipulations block the main thread directly in the render loop on every component re-render.
**Action:** Extract expensive JSX mapping transformations (especially on long arrays like map nodes) into separate `useMemo` hooks (e.g. `const nodeMarkers = useMemo(() => nodes.map(...), [nodes, ...])`) so they only recalculate when their specific dependencies change.
