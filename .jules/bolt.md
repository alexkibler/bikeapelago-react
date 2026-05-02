## 2024-06-25 - Expensive JSON Parsing in Render Loop
**Learning:** Found synchronous `JSON.parse` operations running on potentially large spatial data (polylines) directly inside the React component render loop in `GameView.tsx`. This causes the main thread to block and results in laggy UI interactions whenever the component re-renders (e.g., state updates not related to the polyline).
**Action:** Always extract and memoize `JSON.parse` and array `.map()` transformations of large datasets using `useMemo` so they only recalculate when the source data actually changes.
## 2024-06-25 - Redundant Array Filtering in RoutePanel
**Learning:** Found three separate `O(N)` `filter` calls inside `RoutePanel` to calculate derived node lists, which can severely impact performance for large arrays.
**Action:** Replace multiple `filter` operations on large arrays with a single loop and memoize the result to avoid redundant calculations.
