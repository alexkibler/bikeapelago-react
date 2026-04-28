## 2024-06-25 - Expensive JSON Parsing in Render Loop
**Learning:** Found synchronous `JSON.parse` operations running on potentially large spatial data (polylines) directly inside the React component render loop in `GameView.tsx`. This causes the main thread to block and results in laggy UI interactions whenever the component re-renders (e.g., state updates not related to the polyline).
**Action:** Always extract and memoize `JSON.parse` and array `.map()` transformations of large datasets using `useMemo` so they only recalculate when the source data actually changes.

## 2025-02-12 - Use Random.Shared and Span for shuffling
**Learning:** In C#/.NET 8+, using `new Random()` frequently is less performant and potentially insecure in multi-threaded contexts. `list.OrderBy(_ => rng.Next()).ToList()` allocates multiple times and is O(n log n).
**Action:** Use `Random.Shared` to access a thread-safe random instance. Use `Random.Shared.Shuffle(CollectionsMarshal.AsSpan(list))` for extremely fast, allocation-free, O(n) in-place shuffling of `List<T>`.
