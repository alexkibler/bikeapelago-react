## 2024-03-24 - Accessibility gaps in floating interactive elements
**Learning:** Found that secondary mapping controls (zoom, locate) and floating panels lacked basic ARIA labels despite being primarily icon-driven. This indicates a gap in how accessibility is handled for interactive map overlays compared to standard forms or navigation.
**Action:** When implementing or reviewing new map controls or floating UI overlays (like toasts or side panels), explicitly verify that purely icon-based triggers have semantic `aria-label` attributes configured.

## 2024-05-16 - Accessibility labels and dynamic titles
**Learning:** Text inputs without visible labels (e.g. chat input) need an `aria-label` attribute. Icon-only buttons also require an `aria-label`. Furthermore, disabled buttons benefit greatly from a dynamic `title` attribute to provide context as to why they are disabled (e.g. "Waiting for connection...").
**Action:** When implementing new forms or interactive elements, ensure all inputs have labels (visible or `aria-label`) and that disabled buttons communicate the reason for their disabled state to the user.
