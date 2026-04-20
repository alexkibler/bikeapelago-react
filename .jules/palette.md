
## 2024-05-18 - [Accessibility for Custom Components]
**Learning:** Custom interactive UI elements in React (like custom toggle switches or collapsible headers) built with `div`s, `button`s, and Tailwind CSS need explicit ARIA attributes to be fully accessible. Specifically, custom toggle buttons require `role="switch"` and an `aria-checked` boolean state, while custom accordions or collapsible headers require an `aria-expanded` state.
**Action:** Always verify that interactive non-native UI components have appropriate ARIA roles and state attributes representing their function.
