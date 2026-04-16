## 2024-03-24 - Accessibility gaps in floating interactive elements
**Learning:** Found that secondary mapping controls (zoom, locate) and floating panels lacked basic ARIA labels despite being primarily icon-driven. This indicates a gap in how accessibility is handled for interactive map overlays compared to standard forms or navigation.
**Action:** When implementing or reviewing new map controls or floating UI overlays (like toasts or side panels), explicitly verify that purely icon-based triggers have semantic `aria-label` attributes configured.

## 2024-04-16 - Accessible Custom Switch Patterns
**Learning:** Custom toggle buttons implemented with `div`s or complex CSS instead of native inputs require `role="switch"` and an `aria-checked` boolean state to properly communicate their function and current state to screen readers.
**Action:** Always add `role="switch"` and sync `aria-checked={isToggled}` when building custom toggle switches.
