## 2024-03-24 - Accessibility gaps in floating interactive elements
**Learning:** Found that secondary mapping controls (zoom, locate) and floating panels lacked basic ARIA labels despite being primarily icon-driven. This indicates a gap in how accessibility is handled for interactive map overlays compared to standard forms or navigation.
**Action:** When implementing or reviewing new map controls or floating UI overlays (like toasts or side panels), explicitly verify that purely icon-based triggers have semantic `aria-label` attributes configured.
## 2024-05-23 - Custom Interactive UI Elements Need ARIA

**Learning:** When using custom toggle buttons (`div` or `button` styled as switches) or custom collapsible accordions (like category headers) in React, they don't inherently communicate their state to screen readers unlike native inputs. The `RoutePanel` had a custom switch without `role="switch"` and `aria-checked`, and category headers without `aria-expanded`.
**Action:** Always ensure custom switches have `role="switch"` and `aria-checked={boolean}`, and collapsible sections have `aria-expanded={boolean}` on their toggle buttons to properly communicate their function and state to assistive technologies.
