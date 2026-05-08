## 2024-06-25 - Expensive JSON Parsing in Render Loop
**Learning:** Found synchronous `JSON.parse` operations running on potentially large spatial data (polylines) directly inside the React component render loop in `GameView.tsx`. This causes the main thread to block and results in laggy UI interactions whenever the component re-renders (e.g., state updates not related to the polyline).
**Action:** Always extract and memoize `JSON.parse` and array `.map()` transformations of large datasets using `useMemo` so they only recalculate when the source data actually changes.
## 2024-05-19 - RoutePanel Node Filtering Bottleneck
**Learning:** In `RoutePanel.tsx`, separating state filtering into multiple `nodes.filter()` operations caused redundant full-array passes (O(3N)) on every render, wasting CPU cycles and potentially causing UI lag during unrelated state updates like toggling accordion panels.
**Action:** Replace multiple `.filter()` calls with a single O(N) loop mapped internally using `useMemo` so nodes are only evaluated and bucketed when the main array changes, ensuring UI changes stay smooth.
