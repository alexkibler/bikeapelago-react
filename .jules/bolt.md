## 2024-06-25 - Expensive JSON Parsing in Render Loop
**Learning:** Found synchronous `JSON.parse` operations running on potentially large spatial data (polylines) directly inside the React component render loop in `GameView.tsx`. This causes the main thread to block and results in laggy UI interactions whenever the component re-renders (e.g., state updates not related to the polyline).
**Action:** Always extract and memoize `JSON.parse` and array `.map()` transformations of large datasets using `useMemo` so they only recalculate when the source data actually changes.
## 2024-06-25 - Replace multiple independent array filters with a single O(N) loop
**Learning:** Found multiple independent `.filter()` calls iterating over the same `nodes` array to derive subsets inside the React component render loop in `RoutePanel.tsx`. This creates unnecessary O(N) passes and redundant array allocations that happen on every render.
**Action:** When categorizing a single array into multiple subsets, replace multiple `.filter()` passes with a single loop and memoize the result using `useMemo`.
