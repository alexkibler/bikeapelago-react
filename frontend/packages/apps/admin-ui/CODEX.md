# Admin UI Codex Instructions

Guidance for Codex agents in `packages/apps/admin-ui`.

## Commands

Run from `frontend/`:

```bash
pnpm --filter "@bikeapelago/admin-ui" run dev
pnpm --filter "@bikeapelago/admin-ui" run build
pnpm --filter "@bikeapelago/admin-ui" run tsc
```

## Notes

- API proxy target is from `VITE_PUBLIC_API_URL`/`VITE_API_URL`.
- Update package README and frontend root docs for behavior changes.
