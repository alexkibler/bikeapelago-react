## 2024-03-24 - Accessibility gaps in floating interactive elements
**Learning:** Found that secondary mapping controls (zoom, locate) and floating panels lacked basic ARIA labels despite being primarily icon-driven. This indicates a gap in how accessibility is handled for interactive map overlays compared to standard forms or navigation.
**Action:** When implementing or reviewing new map controls or floating UI overlays (like toasts or side panels), explicitly verify that purely icon-based triggers have semantic `aria-label` attributes configured.
## 2026-04-24 - [Custom Toggle Accessibility]
**Learning:** Unlike native checkboxes, clicking a `<label>` associated with a custom `<button role="switch">` only focuses the button without activating it.
**Action:** To ensure correct behavior, either use a `<span>` with `aria-labelledby` on the button, or use a `<label>` with an `onClick` handler that calls `preventDefault()`, toggles the state manually, and focuses the target element.
