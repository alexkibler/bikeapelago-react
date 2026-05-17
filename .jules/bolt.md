## 2024-06-25 - Expensive JSON Parsing in Render Loop
**Learning:** Found synchronous `JSON.parse` operations running on potentially large spatial data (polylines) directly inside the React component render loop in `GameView.tsx`. This causes the main thread to block and results in laggy UI interactions whenever the component re-renders (e.g., state updates not related to the polyline).
**Action:** Always extract and memoize `JSON.parse` and array `.map()` transformations of large datasets using `useMemo` so they only recalculate when the source data actually changes.
## 2026-05-17 - O(N) Array Operations in Render Loop
**Learning:** Found multiple independent `.filter()` calls executing on potentially large arrays (e.g. `nodes`) directly in the render body. This is O(3N) and triggers main-thread blocking re-calculations on every render, even for unrelated local state updates (like toggling a UI category dropdown).
**Action:** Combine multiple array subsets into a single O(N) pass loop and wrap it in `useMemo` to prevent main thread blocking and redundant processing.
