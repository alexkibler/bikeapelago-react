## 2024-06-25 - Expensive JSON Parsing in Render Loop
**Learning:** Found synchronous `JSON.parse` operations running on potentially large spatial data (polylines) directly inside the React component render loop in `GameView.tsx`. This causes the main thread to block and results in laggy UI interactions whenever the component re-renders (e.g., state updates not related to the polyline).
**Action:** Always extract and memoize `JSON.parse` and array `.map()` transformations of large datasets using `useMemo` so they only recalculate when the source data actually changes.

## 2024-06-26 - Optimize React render performance for deriving multiple subsets
**Learning:** In React components like `RoutePanel`, deriving multiple independent subsets from a large array using consecutive `.filter()` calls (e.g., categorizing nodes by state) performs $O(N)$ operations for each subset and triggers redundant array allocations during every render, which can block the main thread.
**Action:** Always replace multiple independent `.filter()` calls with a single $O(N)$ loop wrapped in a `useMemo` hook when categorizing or deriving subsets from large datasets to minimize redundant operations and avoid main thread blocking.
