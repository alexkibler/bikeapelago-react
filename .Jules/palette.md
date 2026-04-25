## 2024-03-24 - Accessibility gaps in floating interactive elements
**Learning:** Found that secondary mapping controls (zoom, locate) and floating panels lacked basic ARIA labels despite being primarily icon-driven. This indicates a gap in how accessibility is handled for interactive map overlays compared to standard forms or navigation.
**Action:** When implementing or reviewing new map controls or floating UI overlays (like toasts or side panels), explicitly verify that purely icon-based triggers have semantic `aria-label` attributes configured.
## 2026-04-25 - Custom Toggle Checkbox Accessibility
**Learning:** In this application's custom UI implementation, associating a native `<label>` with a custom button (`role='switch'`) using `htmlFor` is insufficient for full interactivity, as clicking the label merely focuses the button without toggling its state. Furthermore, a unique ID is required to link them properly.
**Action:** Use React's `useId()` to guarantee a unique ID for the relationship, and add an `onClick` handler to the label to manually trigger the state change and focus the element.
