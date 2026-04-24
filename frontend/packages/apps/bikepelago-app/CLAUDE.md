# Game App Claude Instructions

Guidance for Claude in `packages/apps/bikepelago-app`.

## Commands

Run from `frontend/`:

```bash
pnpm --filter "@bikeapelago/bikepelago-app" run dev
pnpm --filter "@bikeapelago/bikepelago-app" run build
pnpm --filter "@bikeapelago/bikepelago-app" run lint
pnpm --filter "@bikeapelago/bikepelago-app" run test:run
```

## Working Rules

- Use functional React components and hooks.
- Keep Zustand store contracts stable unless change is intentional.
- Keep API calls compatible with current backend routes.
- Keep tests updated for changed UI behavior.
