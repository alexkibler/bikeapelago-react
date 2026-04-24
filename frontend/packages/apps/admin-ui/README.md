# Bikeapelago Admin UI (`@bikeapelago/admin-ui`)

Administrative dashboard for Bikeapelago.

## Commands

Run from `frontend/`:

```bash
pnpm --filter "@bikeapelago/admin-ui" run dev
pnpm --filter "@bikeapelago/admin-ui" run build
pnpm --filter "@bikeapelago/admin-ui" run tsc
```

## Environment

Optional `.env` in this folder:

```env
VITE_PUBLIC_API_URL=http://localhost:5054
```

If unset, Vite falls back to `VITE_API_URL`, then `http://127.0.0.1:5054`.

## Related Docs

- [Frontend README](../../README.md)
- [Root README](../../../../README.md)
- [Claude instructions](CLAUDE.md)
- [Gemini instructions](GEMINI.md)
- [Codex instructions](CODEX.md)
