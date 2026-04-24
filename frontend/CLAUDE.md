# Frontend Claude Instructions

Guidance for Claude working inside `frontend/`.

## Commands

```bash
pnpm install
pnpm -r --if-present run build
pnpm -r --if-present run lint
pnpm --filter "@bikeapelago/bikepelago-app" run dev
pnpm --filter "@bikeapelago/bikepelago-app" run test:run
pnpm --filter "@bikeapelago/admin-ui" run dev
pnpm --filter "@bikeapelago/admin-ui" run tsc
```

## Working Rules

- Use pnpm workspace commands (not npm).
- Keep edits scoped to relevant app/package.
- Preserve shared package boundaries under `packages/shared/*`.
- Update docs when scripts/config change.
