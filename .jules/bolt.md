## 2024-06-25 - Expensive JSON Parsing in Render Loop
**Learning:** Found synchronous `JSON.parse` operations running on potentially large spatial data (polylines) directly inside the React component render loop in `GameView.tsx`. This causes the main thread to block and results in laggy UI interactions whenever the component re-renders (e.g., state updates not related to the polyline).
**Action:** Always extract and memoize `JSON.parse` and array `.map()` transformations of large datasets using `useMemo` so they only recalculate when the source data actually changes.

## 2026-05-11 - Array processing in render loops
**Learning:** Found independent `.filter()` calls on large arrays inside component render loops (e.g., `RoutePanel.tsx`). This results in multiple O(N) iterations and redundant allocations that block the main thread unnecessarily during any state change.
**Action:** Replaced multiple independent `.filter()` calls with a single O(N) loop wrapped in `useMemo` to iterate once and correctly categorize items, preventing redundant processing.
