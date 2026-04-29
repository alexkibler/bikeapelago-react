## 2024-06-25 - Expensive JSON Parsing in Render Loop
**Learning:** Found synchronous `JSON.parse` operations running on potentially large spatial data (polylines) directly inside the React component render loop in `GameView.tsx`. This causes the main thread to block and results in laggy UI interactions whenever the component re-renders (e.g., state updates not related to the polyline).
**Action:** Always extract and memoize `JSON.parse` and array `.map()` transformations of large datasets using `useMemo` so they only recalculate when the source data actually changes.

## 2025-04-29 - [RoutePanel Array Traversal Bottleneck]
**Learning:** Performing multiple independent `.filter()` operations on a large context-provided array (`nodes`) during every render cycle causes significant main-thread blocking, particularly when the component also handles frequent local UI state updates (like toggling accordion categories).
**Action:** Replace multiple `.filter()` calls with a single $O(N)$ loop wrapped in a `useMemo` block that buckets the array into the necessary subsets, effectively preventing $O(3N)$ recalculations and minimizing garbage collection overhead.
