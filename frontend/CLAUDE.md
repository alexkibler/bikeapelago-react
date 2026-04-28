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
- Prefer RTK Query for new frontend tool/API calls wherever RTK is available in the target app.
- Reuse existing shared request utilities or local fetch/query patterns when adding RTK would be out of scope for the change, but avoid introducing new ad hoc request code when an RTK endpoint can cover it.
- Update docs when scripts/config change.
