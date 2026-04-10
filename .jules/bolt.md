## 2024-04-10 - Memoizing Large JSON Spatial Data
**Learning:** React render loops can be synchronously blocked by parsing large JSON strings (like map polylines) if not memoized. In the `GameView` component, rendering was noticeably slow because `JSON.parse` was executing on every render for potentially massive spatial coordinates.
**Action:** Always wrap expensive operations like `JSON.parse` for large string data structures inside a `useMemo` hook, ensuring it only runs when the underlying data string actually changes.
