# Frontend Codex Instructions

Guidance for Codex agents working in `frontend/`.

## Workspace Overview

- `packages/apps/bikepelago-app` - user-facing game app
- `packages/apps/admin-ui` - admin dashboard
- `packages/shared/*` - shared configs/components/utilities

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
