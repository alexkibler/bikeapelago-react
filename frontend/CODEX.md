# Frontend Codex Instructions

Guidance for Codex agents working in `frontend/`.

## Workspace Overview

- `packages/apps/bikepelago-app` - user-facing game app
- `packages/apps/admin-ui` - admin dashboard
- `packages/shared/*` - shared configs/components/utilities

## Data Fetching

- Prefer RTK Query for new frontend tool/API calls wherever RTK is available in the target app.
- Reuse existing shared request utilities or local fetch/query patterns when adding RTK would be out of scope for the change, but avoid introducing new ad hoc request code when an RTK endpoint can cover it.

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

## Documentation Contract

When frontend behavior changes, update:
- `frontend/README.md`
- relevant app docs under `packages/apps/*`
