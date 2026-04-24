# Admin UI Claude Instructions

Guidance for Claude in `packages/apps/admin-ui`.

## Commands

Run from `frontend/`:

```bash
pnpm --filter "@bikeapelago/admin-ui" run dev
pnpm --filter "@bikeapelago/admin-ui" run build
pnpm --filter "@bikeapelago/admin-ui" run tsc
```

## Rules

- Keep React + TypeScript strict and explicit.
- Avoid introducing dependencies unless required.
- Update docs when command or env behavior changes.
