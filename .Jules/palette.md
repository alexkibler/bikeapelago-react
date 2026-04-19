## 2024-03-24 - Accessibility gaps in floating interactive elements
**Learning:** Found that secondary mapping controls (zoom, locate) and floating panels lacked basic ARIA labels despite being primarily icon-driven. This indicates a gap in how accessibility is handled for interactive map overlays compared to standard forms or navigation.
**Action:** When implementing or reviewing new map controls or floating UI overlays (like toasts or side panels), explicitly verify that purely icon-based triggers have semantic `aria-label` attributes configured.
## 2026-04-19 - Adding ARIA attributes to custom interactive elements
**Learning:** Custom built switch and accordion components can easily lack proper accessibility without `role="switch"`, `aria-checked`, and `aria-expanded` attributes.
**Action:** Ensure any custom interactive UI element such as toggles or accordions accurately maps its internal state variable to the appropriate ARIA attribute to be fully accessible.
