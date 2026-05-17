## 2024-03-24 - Accessibility gaps in floating interactive elements
**Learning:** Found that secondary mapping controls (zoom, locate) and floating panels lacked basic ARIA labels despite being primarily icon-driven. This indicates a gap in how accessibility is handled for interactive map overlays compared to standard forms or navigation.
**Action:** When implementing or reviewing new map controls or floating UI overlays (like toasts or side panels), explicitly verify that purely icon-based triggers have semantic `aria-label` attributes configured.
## 2024-05-17 - Panel Toggle Accessibility
**Learning:** Panel toggle buttons in responsive layouts (like sidebars and bottom navs) often act as visually distinct stateful buttons but fail to convey their current active/expanded state to screen readers. Relying purely on visual color/background changes is an accessibility anti-pattern.
**Action:** When implementing collapsible side panels or bottom sheets, ensure toggle buttons include the `aria-expanded` attribute dynamically bound to their active state to provide proper contextual feedback.
