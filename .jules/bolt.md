## 2024-06-25 - Expensive JSON Parsing in Render Loop
**Learning:** Found synchronous `JSON.parse` operations running on potentially large spatial data (polylines) directly inside the React component render loop in `GameView.tsx`. This causes the main thread to block and results in laggy UI interactions whenever the component re-renders (e.g., state updates not related to the polyline).
**Action:** Always extract and memoize `JSON.parse` and array `.map()` transformations of large datasets using `useMemo` so they only recalculate when the source data actually changes.
## 2026-05-07 - Optimize Collection Shuffling
**Learning:** In C#, replacing `OrderBy(_ => new Random().Next())` or manual Fisher-Yates loops with `Random.Shared.Shuffle(CollectionsMarshal.AsSpan(list))` reduces algorithmic complexity from O(N log N) to O(N) and eliminates memory allocations, preventing potential GC pressure and non-thread-safe local `Random` bugs.
**Action:** Always prefer `Random.Shared.Shuffle(CollectionsMarshal.AsSpan(list))` when shuffling generic `List<T>` collections in .NET 8+ backends.
