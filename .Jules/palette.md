## 2024-03-24 - Accessibility gaps in floating interactive elements
**Learning:** Found that secondary mapping controls (zoom, locate) and floating panels lacked basic ARIA labels despite being primarily icon-driven. This indicates a gap in how accessibility is handled for interactive map overlays compared to standard forms or navigation.
**Action:** When implementing or reviewing new map controls or floating UI overlays (like toasts or side panels), explicitly verify that purely icon-based triggers have semantic `aria-label` attributes configured.

## 2024-05-10 - Form Inputs and Icon Buttons
**Learning:** The chat panel input lacked an aria-label, and its purely icon-based submit button lacked both an aria-label and a title explaining its disabled state. This reduces accessibility for screen readers and makes it hard for users to know why the button is disabled.
**Action:** Ensure all form inputs without explicit labels have an aria-label, and all icon-only buttons have an aria-label and a title describing their action and state.
