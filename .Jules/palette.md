## 2024-03-24 - Accessibility gaps in floating interactive elements
**Learning:** Found that secondary mapping controls (zoom, locate) and floating panels lacked basic ARIA labels despite being primarily icon-driven. This indicates a gap in how accessibility is handled for interactive map overlays compared to standard forms or navigation.
**Action:** When implementing or reviewing new map controls or floating UI overlays (like toasts or side panels), explicitly verify that purely icon-based triggers have semantic `aria-label` attributes configured.
## 2024-05-11 - Add ARIA label and dynamic title to icon-only send button
**Learning:** Found a chat send button that lacked both an `aria-label` and a dynamic `title` explaining its disabled state to the user. This made it inaccessible to screen readers and potentially confusing to sighted users when disconnected.
**Action:** Always provide `aria-label`s for icon-only buttons, and use dynamic `title` attributes on disabled buttons to give context on *why* they are disabled (e.g., "Waiting for connection...").
