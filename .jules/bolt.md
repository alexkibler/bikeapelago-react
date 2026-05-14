## 2024-06-25 - Expensive JSON Parsing in Render Loop
**Learning:** Found synchronous `JSON.parse` operations running on potentially large spatial data (polylines) directly inside the React component render loop in `GameView.tsx`. This causes the main thread to block and results in laggy UI interactions whenever the component re-renders (e.g., state updates not related to the polyline).
**Action:** Always extract and memoize `JSON.parse` and array `.map()` transformations of large datasets using `useMemo` so they only recalculate when the source data actually changes.

## 2026-05-14 - Array filtering optimization in render loops
**Learning:** Found O(N) array filtering operations being performed sequentially within a React render loop in `RoutePanel.tsx`, causing main-thread blocking UI work.
**Action:** Use a single O(N) pass wrapped in a `useMemo` hook to sort arrays into respective bins simultaneously, optimizing operations inside frequent rendering paths.
