## 2024-06-25 - Expensive JSON Parsing in Render Loop
**Learning:** Found synchronous `JSON.parse` operations running on potentially large spatial data (polylines) directly inside the React component render loop in `GameView.tsx`. This causes the main thread to block and results in laggy UI interactions whenever the component re-renders (e.g., state updates not related to the polyline).
**Action:** Always extract and memoize `JSON.parse` and array `.map()` transformations of large datasets using `useMemo` so they only recalculate when the source data actually changes.
## 2025-04-30 - O(N) Array Iteration & Memoization
**Learning:** Performing multiple `.filter()` passes over large arrays inside React components blocks the main thread excessively.
**Action:** Replace multiple `.filter()` calls with a single $O(N)$ loop wrapped inside a `useMemo` hook to combine parsing/categorization on arrays whenever possible, reducing time complexity and maintaining React UI responsiveness.
