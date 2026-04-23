## 2024-06-25 - Expensive JSON Parsing in Render Loop
**Learning:** Found synchronous `JSON.parse` operations running on potentially large spatial data (polylines) directly inside the React component render loop in `GameView.tsx`. This causes the main thread to block and results in laggy UI interactions whenever the component re-renders (e.g., state updates not related to the polyline).
**Action:** Always extract and memoize `JSON.parse` and array `.map()` transformations of large datasets using `useMemo` so they only recalculate when the source data actually changes.

## 2024-06-25 - Redundant Array Serialization for GPX Downloads
**Learning:** The `downloadGPXFromPolyline` function in `geoUtils.ts` previously accepted a JSON string, forcing the caller to `JSON.stringify()` an existing array, which the function immediately had to `JSON.parse()`. This caused an unnecessary O(N) serialization/deserialization cycle that blocks the main thread, especially problematic since route polyline data can be large.
**Action:** When a function requires array or object data, pass it directly by reference instead of converting to strings and parsing, bypassing expensive serialization operations.
