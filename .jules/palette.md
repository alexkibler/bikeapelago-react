## 2026-05-07 - Improve Chat Input Accessibility and Feedback
**Learning:** Icon-only buttons (like the send button) and text inputs without visible labels cause accessibility and usability issues. A disabled button without context leaves users confused about why they cannot interact with it.
**Action:** Next time, always ensure text inputs have either an associated `<label>` or an `aria-label`, provide `aria-label` for icon-only buttons, and use dynamic `title` attributes to explain why an element might be disabled.
