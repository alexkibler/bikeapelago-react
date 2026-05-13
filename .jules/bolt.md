## 2024-06-25 - Expensive JSON Parsing in Render Loop
**Learning:** Found synchronous `JSON.parse` operations running on potentially large spatial data (polylines) directly inside the React component render loop in `GameView.tsx`. This causes the main thread to block and results in laggy UI interactions whenever the component re-renders (e.g., state updates not related to the polyline).
**Action:** Always extract and memoize `JSON.parse` and array `.map()` transformations of large datasets using `useMemo` so they only recalculate when the source data actually changes.

## 2024-07-25 - Redundant Array Traversals in Render Loops
**Learning:** Performing multiple independent `.filter()` operations on a large array (like map nodes) directly in a React component's render loop causes redundant O(N) traversals and array allocations. This can block the main thread and degrade performance during frequent, unrelated state updates.
**Action:** Replace multiple `.filter()` calls that derive subsets from the same source array with a single loop inside a `useMemo` block. This reduces time complexity from O(M*N) to O(N) and prevents unnecessary re-calculations.
