# Bikeapelago: Frontend Guidelines (Claude)

Instructions for Claude working on the React frontend.

## Build and Test Commands
- **Install**: `npm install`
- **Dev**: `npm run dev`
- **Build**: `npm run build`
- **Lint**: `npm run lint --fix`
- **E2E Tests**: `npm run test:e2e`
- **E2E Individual**: `npx playwright test tests/e2e/specific_test.spec.ts`

## Style Guidelines
- **Components**: Functional components with TypeScript interfaces for props.
- **State**: Centralized in Zustand stores under `src/store/`.
- **Styling**: Tailwind CSS with DaisyUI components.
- **Hooks**: Abstract complex logic into custom hooks under `src/hooks/`.
- **Testing**: Prioritize E2E coverage with Playwright for core user flows.
- **Types**: Strictly typed with TypeScript; avoid `any`.
