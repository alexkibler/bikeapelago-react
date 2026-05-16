## 2024-06-25 - Expensive JSON Parsing in Render Loop
**Learning:** Found synchronous `JSON.parse` operations running on potentially large spatial data (polylines) directly inside the React component render loop in `GameView.tsx`. This causes the main thread to block and results in laggy UI interactions whenever the component re-renders (e.g., state updates not related to the polyline).
**Action:** Always extract and memoize `JSON.parse` and array `.map()` transformations of large datasets using `useMemo` so they only recalculate when the source data actually changes.

## 2024-06-25 - Redundant Array Filtering in Render
**Learning:** In React components dealing with large collections (like MapNodes), running multiple `.filter()` operations consecutively inside the render function scales poorly and creates redundant array allocations, leading to jank when unrelated state variables update.
**Action:** Combine multiple array traversals into a single (N)$ loop wrapped in `useMemo` to drastically reduce allocations and improve render stability.
