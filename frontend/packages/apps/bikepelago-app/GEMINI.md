# Game App Gemini Instructions

Guidance for Gemini in `packages/apps/bikepelago-app`.

## Build and Validation

Run from `frontend/`:

```bash
pnpm --filter "@bikeapelago/bikepelago-app" run build
pnpm --filter "@bikeapelago/bikepelago-app" run lint
pnpm --filter "@bikeapelago/bikepelago-app" run test:run
```

## Development

```bash
pnpm --filter "@bikeapelago/bikepelago-app" run dev
```

## Rules

- Prefer targeted edits to existing components.
- Preserve TypeScript typing; avoid `any`.
- Update README if scripts/config behavior changes.
