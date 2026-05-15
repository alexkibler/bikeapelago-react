## 2024-03-24 - Dynamic Tooltips for Disabled States
**Learning:** For icon-only buttons that may be disabled due to complex state conditions (e.g. waiting for connection or empty input), just greying them out is often insufficient. Adding a dynamic `title` attribute that explicitly explains *why* the button is disabled improves both clarity and accessibility.
**Action:** When implementing interactive buttons with variable disabled states, provide context-aware feedback (e.g. via `title` or tooltip) alongside the visual styling.
