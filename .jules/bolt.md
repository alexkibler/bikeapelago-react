## 2024-06-25 - Expensive JSON Parsing in Render Loop
**Learning:** Found synchronous `JSON.parse` operations running on potentially large spatial data (polylines) directly inside the React component render loop in `GameView.tsx`. This causes the main thread to block and results in laggy UI interactions whenever the component re-renders (e.g., state updates not related to the polyline).
**Action:** Always extract and memoize `JSON.parse` and array `.map()` transformations of large datasets using `useMemo` so they only recalculate when the source data actually changes.

## 2024-05-18 - Replacing multiple `.filter()` calls with a single O(N) loop and memoizing
**Learning:** Found multiple independent `.filter()` calls applied to the same large array (e.g., categorizing or counting `nodes`) directly in the render loop. This leads to 2x or 3x iterations (O(K*N)) and re-calculates the results on every component render, which can needlessly block the main thread.
**Action:** When deriving multiple subsets or counts from a large array, replace multiple independent `.filter()` calls with a single O(N) loop (using `for...of` or `reduce`) and wrap the logic in a `useMemo` hook to prevent redundant allocations and re-calculations on unrelated state updates.
