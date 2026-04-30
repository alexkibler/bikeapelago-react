## 2024-03-24 - Accessibility gaps in floating interactive elements
**Learning:** Found that secondary mapping controls (zoom, locate) and floating panels lacked basic ARIA labels despite being primarily icon-driven. This indicates a gap in how accessibility is handled for interactive map overlays compared to standard forms or navigation.
**Action:** When implementing or reviewing new map controls or floating UI overlays (like toasts or side panels), explicitly verify that purely icon-based triggers have semantic `aria-label` attributes configured.

## 2024-04-30 - Chat Form Accessibility
**Learning:** Found an interactive form in ChatPanel lacking both an explicit label (or aria-label) for its text input and an aria-label for its icon-only submit button. Such patterns silently degrade screen reader experience.
**Action:** Always verify that input fields without visible labels have an `aria-label` and ensure icon-only buttons clearly announce their intent to assistive tech. Additionally, provide clear `focus-visible` styling for interactive elements to aid keyboard navigation.
