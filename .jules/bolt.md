## 2024-06-25 - Expensive JSON Parsing in Render Loop
**Learning:** Found synchronous `JSON.parse` operations running on potentially large spatial data (polylines) directly inside the React component render loop in `GameView.tsx`. This causes the main thread to block and results in laggy UI interactions whenever the component re-renders (e.g., state updates not related to the polyline).
**Action:** Always extract and memoize `JSON.parse` and array `.map()` transformations of large datasets using `useMemo` so they only recalculate when the source data actually changes.
## $(date +%Y-%m-%d) - Optimize derived state array iteration
**Learning:** In React components managing large lists (like game map nodes), deriving multiple sub-states via separate `.filter()` operations can cause significant unnecessary iteration (e.g. O(3N)) on every render.
**Action:** When categorizing large arrays into multiple sub-lists based on a discriminator (like node state), use a single pass O(N) loop and memoize the resulting object with `useMemo` to prevent both redundant array allocations and main-thread blocking during unrelated local state updates.
