## 2024-03-24 - Accessibility gaps in floating interactive elements
**Learning:** Found that secondary mapping controls (zoom, locate) and floating panels lacked basic ARIA labels despite being primarily icon-driven. This indicates a gap in how accessibility is handled for interactive map overlays compared to standard forms or navigation.
**Action:** When implementing or reviewing new map controls or floating UI overlays (like toasts or side panels), explicitly verify that purely icon-based triggers have semantic `aria-label` attributes configured.

## 2026-05-04 - Accessibility & Focus states on Icon-Only Buttons
**Learning:** Icon-only buttons (like the Chat Send and Upload File buttons) need clear `aria-label`, `title` for tooltips, and explicit `focus-visible` styles (e.g. `focus-visible:ring-2`) for screen readers and keyboard users.
**Action:** Always check interactive icon-only elements to ensure they have an accessible name and explicit visible focus state in the UI.
