## 2024-06-25 - Expensive JSON Parsing in Render Loop
**Learning:** Found synchronous `JSON.parse` operations running on potentially large spatial data (polylines) directly inside the React component render loop in `GameView.tsx`. This causes the main thread to block and results in laggy UI interactions whenever the component re-renders (e.g., state updates not related to the polyline).
**Action:** Always extract and memoize `JSON.parse` and array `.map()` transformations of large datasets using `useMemo` so they only recalculate when the source data actually changes.

## 2024-08-01 - Consolidating Multiple Array Filters
**Learning:** In React components like `RoutePanel` and `GameStatsBar`, deriving multiple subsets or aggregates from a large array (e.g. `nodes`) using multiple independent `.filter()` calls loops over the array redundantly, creating multiple O(N) operations and temporary allocations. During frequent state updates, this can cumulatively impact render performance.
**Action:** Replace multiple independent `.filter()` operations with a single O(N) loop wrapped in a `useMemo` hook. This groups the computations into a single pass, avoiding redundant iterations and caching the result to skip processing entirely when the source array hasn't changed.
