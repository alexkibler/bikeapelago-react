# Bikeapelago Frontend Workspace (`frontend/`)

pnpm workspace containing both UI applications:
- `@bikeapelago/bikeapelago-app` (main game app)
- `@bikeapelago/admin-ui` (admin dashboard)

## Workspace Structure

```text
frontend/
├── package.json
├── pnpm-workspace.yaml
└── packages/
    ├── apps/
    │   ├── bikepelago-app/
    │   └── admin-ui/
    └── shared/
```

## Commands

Use the root [README](../README.md) for install, run, build, validation, and Docker commands.

## Environment Configuration

Set API base URL per app via `.env` files:

- `packages/apps/bikepelago-app/.env`
- `packages/apps/admin-ui/.env`

Both apps support `VITE_PUBLIC_API_URL` for browser/native runtime API origin and `VITE_API_URL` for the Vite dev proxy target. Local `.env.local` files are app-local because Vite loads env files from each app directory.

## Docker (Production-style Frontends)

Build runtime images from `frontend/Dockerfile` targets:
- `runtime-frontend`
- `runtime-admin-ui`

At runtime, Nginx templates use:
- `API_PROXY_URL`
- `API_HUBS_URL`

See the root [README](../README.md) and compose files for deployment wiring.

## Related Docs

- [Root README](../README.md)
- [Game App README](packages/apps/bikepelago-app/README.md)
- [Admin UI README](packages/apps/admin-ui/README.md)
- [Frontend Claude instructions](CLAUDE.md)
- [Frontend Gemini instructions](GEMINI.md)
- [Frontend Codex instructions](CODEX.md)
