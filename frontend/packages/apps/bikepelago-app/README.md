# Bikeapelago Game App (`@bikeapelago/bikepelago-app`)

Main player-facing React application.

## Commands

From `frontend/`:

```bash
pnpm --filter "@bikeapelago/bikepelago-app" run dev
pnpm --filter "@bikeapelago/bikepelago-app" run build
pnpm --filter "@bikeapelago/bikepelago-app" run lint
pnpm --filter "@bikeapelago/bikepelago-app" run test
pnpm --filter "@bikeapelago/bikepelago-app" run test:run
```

## Environment

Optional `.env` in this folder:

```env
VITE_PUBLIC_API_URL=http://localhost:5054
```

If unset, Vite falls back to `VITE_API_URL`, then `http://127.0.0.1:5054`.

## Notes

- `build` and `dev` both run `build:apworld` before Vite.
- Unit/component tests use Vitest (`src/components/game/__tests__/`).

## Related Docs

- [Frontend README](../../README.md)
- [Root README](../../../../README.md)
- [Claude instructions](CLAUDE.md)
- [Gemini instructions](GEMINI.md)
- [Codex instructions](CODEX.md)
