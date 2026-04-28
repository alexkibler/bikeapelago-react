# Game App Codex Instructions

Guidance for Codex agents in `packages/apps/bikepelago-app`.

## Commands

Run from `frontend/`:

```bash
pnpm --filter "@bikeapelago/bikepelago-app" run dev
pnpm --filter "@bikeapelago/bikepelago-app" run build
pnpm --filter "@bikeapelago/bikepelago-app" run lint
pnpm --filter "@bikeapelago/bikepelago-app" run test:run
```

## Notes

- `dev` and `build` execute `build:apworld` first.
- API proxy target comes from `VITE_PUBLIC_API_URL` / `VITE_API_URL`.
- Prefer RTK Query for new tool/API calls wherever possible; fall back to the existing shared data-fetching helpers or local fetch pattern only when RTK is not wired for the touched area.
- Keep docs/tests in sync with behavior changes.
