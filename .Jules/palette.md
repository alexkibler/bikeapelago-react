## 2024-03-24 - Accessibility gaps in floating interactive elements
**Learning:** Found that secondary mapping controls (zoom, locate) and floating panels lacked basic ARIA labels despite being primarily icon-driven. This indicates a gap in how accessibility is handled for interactive map overlays compared to standard forms or navigation.
**Action:** When implementing or reviewing new map controls or floating UI overlays (like toasts or side panels), explicitly verify that purely icon-based triggers have semantic `aria-label` attributes configured.
## 2024-05-14 - Contextual titles for disabled icon buttons
**Learning:** Icon-only buttons (such as chat submit controls) that can be disabled due to different states (like disconnected vs empty input) benefit greatly from dynamic `title` attributes. This provides context to users about *why* the button is disabled, improving usability.
**Action:** When adding `aria-label` to icon-only interactive elements, also consider if the element can be disabled. If so, implement a dynamic `title` that reflects the exact reason it is disabled, alongside the semantic `aria-label` for screen readers.
