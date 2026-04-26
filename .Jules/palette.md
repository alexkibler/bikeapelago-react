## 2024-03-24 - Accessibility gaps in floating interactive elements
**Learning:** Found that secondary mapping controls (zoom, locate) and floating panels lacked basic ARIA labels despite being primarily icon-driven. This indicates a gap in how accessibility is handled for interactive map overlays compared to standard forms or navigation.
**Action:** When implementing or reviewing new map controls or floating UI overlays (like toasts or side panels), explicitly verify that purely icon-based triggers have semantic `aria-label` attributes configured.
## 2026-04-26 - Toggle Component Accessibility Issue
**Learning:** The custom `<Toggle>` component in this app linked a standard `<label>` to a custom `<button role="switch">`. Clicking the label focused the button but didn't toggle its state across all browsers. Also, it lacked a fallback ID when one wasn't provided, which could break the `htmlFor` linkage.
**Action:** When building or updating custom form controls, always provide a fallback ID using `useId()` and ensure labels properly activate the control's `onClick` handler with `e.preventDefault()` if native label clicking doesn't trigger the associated custom element correctly.
