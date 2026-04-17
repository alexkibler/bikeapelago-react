## 2024-03-24 - Accessibility gaps in floating interactive elements
**Learning:** Found that secondary mapping controls (zoom, locate) and floating panels lacked basic ARIA labels despite being primarily icon-driven. This indicates a gap in how accessibility is handled for interactive map overlays compared to standard forms or navigation.
**Action:** When implementing or reviewing new map controls or floating UI overlays (like toasts or side panels), explicitly verify that purely icon-based triggers have semantic `aria-label` attributes configured.
## 2026-04-17 - [Beware of global format tools]
**Learning:** Running `pnpm run format` from the frontend root reformats many untouched files, creating massive diffs. Avoid workspace-wide formatting to keep PRs strictly under 50 lines.
**Action:** Use `git diff` to verify that only the expected files are modified. Revert any unintended formatting changes using `git checkout` or `git restore` before committing.
