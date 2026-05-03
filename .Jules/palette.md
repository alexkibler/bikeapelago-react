## 2024-03-24 - Accessibility gaps in floating interactive elements
**Learning:** Found that secondary mapping controls (zoom, locate) and floating panels lacked basic ARIA labels despite being primarily icon-driven. This indicates a gap in how accessibility is handled for interactive map overlays compared to standard forms or navigation.
**Action:** When implementing or reviewing new map controls or floating UI overlays (like toasts or side panels), explicitly verify that purely icon-based triggers have semantic `aria-label` attributes configured.
## 2026-05-03 - Added ARIA labels to icon-only buttons
**Learning:** Icon-only buttons lacking `aria-label` attributes are a common accessibility issue that screen readers cannot interpret correctly. In this app, several components like `UploadPanel` and `ChatPanel` contained such buttons.
**Action:** When adding new icon-only interactive elements in the future, always ensure they have a descriptive `aria-label` to maintain accessibility.
