## 2024-03-24 - Accessibility gaps in floating interactive elements
**Learning:** Found that secondary mapping controls (zoom, locate) and floating panels lacked basic ARIA labels despite being primarily icon-driven. This indicates a gap in how accessibility is handled for interactive map overlays compared to standard forms or navigation.
**Action:** When implementing or reviewing new map controls or floating UI overlays (like toasts or side panels), explicitly verify that purely icon-based triggers have semantic `aria-label` attributes configured.

## 2024-04-28 - Custom Switch Button Label Activation
**Learning:** Clicking a `<label>` associated with a custom switch component (`role="switch"` using a `<button>`) merely focuses the button without activating its `checked` state, unlike native checkboxes. Furthermore, when these custom components are instantiated without explicit `id` props, their labels are entirely unlinked.
**Action:** When creating custom inputs, use React's `useId()` to generate a fallback ID to ensure `htmlFor` remains functional. For custom buttons acting as inputs, apply an `onClick` handler to the `<label>` that calls `preventDefault()`, toggles the state, and explicitly focuses the input element.
