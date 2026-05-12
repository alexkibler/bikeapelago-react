## 2024-06-25 - Expensive JSON Parsing in Render Loop
**Learning:** Found synchronous `JSON.parse` operations running on potentially large spatial data (polylines) directly inside the React component render loop in `GameView.tsx`. This causes the main thread to block and results in laggy UI interactions whenever the component re-renders (e.g., state updates not related to the polyline).
**Action:** Always extract and memoize `JSON.parse` and array `.map()` transformations of large datasets using `useMemo` so they only recalculate when the source data actually changes.
## 2024-07-26 - O(N) Array Operations in Render Loop
**Learning:** Performing multiple independent `O(N)` array `.filter()` operations (like deriving node categories) within the render cycle severely degrades performance as the array size scales, blocking the main thread during unrelated component state updates.
**Action:** Replace multiple `.filter()` calls with a single-pass `useMemo` block that iterates over the data once to compute all needed derived arrays or frequency maps, reducing time complexity and preventing unnecessary re-calculations.
