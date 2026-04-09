## 2026-04-09 - Memoize expensive parsing in render loop
**Learning:** Found a performance bottleneck in `GameView.tsx` where a large GPS track represented as a JSON string (`routeData.polyline`) was being passed to `JSON.parse` and mapped over synchronously on every render. This was blocking the main thread during simple UI interactions (like side panels toggling).
**Action:** Always wrap expensive data transformations—especially `JSON.parse` of potentially large arrays—inside `useMemo` hooks. This ensures they only re-run when the underlying raw data changes.
