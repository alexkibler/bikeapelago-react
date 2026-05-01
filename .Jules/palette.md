## 2024-03-24 - Accessibility gaps in floating interactive elements
**Learning:** Found that secondary mapping controls (zoom, locate) and floating panels lacked basic ARIA labels despite being primarily icon-driven. This indicates a gap in how accessibility is handled for interactive map overlays compared to standard forms or navigation.
**Action:** When implementing or reviewing new map controls or floating UI overlays (like toasts or side panels), explicitly verify that purely icon-based triggers have semantic `aria-label` attributes configured.
## 2024-05-19 - Avoid redundant ARIA labels
**Learning:** Adding `aria-label` to buttons or links that already contain visible text (even if small) overrides that visible text for screen readers, which is an accessibility anti-pattern.
**Action:** Only add `aria-label` to strictly icon-only buttons. If an element has visible text, let the screen reader read that text.
