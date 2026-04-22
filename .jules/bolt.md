## 2024-06-25 - Expensive JSON Parsing in Render Loop
**Learning:** Found synchronous `JSON.parse` operations running on potentially large spatial data (polylines) directly inside the React component render loop in `GameView.tsx`. This causes the main thread to block and results in laggy UI interactions whenever the component re-renders (e.g., state updates not related to the polyline).
**Action:** Always extract and memoize `JSON.parse` and array `.map()` transformations of large datasets using `useMemo` so they only recalculate when the source data actually changes.
## 2024-06-25 - Avoid Stringifying Arrays for Function Calls
**Learning:** Found unnecessary `JSON.stringify()` passing a large array (polyline coordinates) to a utility function `downloadGPXFromPolyline`, which then immediately ran `JSON.parse()`. This causes pointless serializing/deserializing of potentially large datasets on the main thread.
**Action:** Refactor utility function signatures to accept arrays/objects directly instead of serialized JSON strings to skip the parsing overhead altogether.
