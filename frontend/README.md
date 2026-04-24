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

## Prerequisites

- Node.js 20+
- pnpm 10+
- API running at `http://localhost:5054` (or configure `VITE_PUBLIC_API_URL`)

## Install

```bash
cd frontend
pnpm install
```

## Development

Game app:

```bash
pnpm --filter "@bikeapelago/bikepelago-app" run dev
```

Admin UI:

```bash
pnpm --filter "@bikeapelago/admin-ui" run dev
```

Both:

```bash
pnpm -r --if-present run dev
```

## Build

All workspace packages:

```bash
pnpm -r --if-present run build
```

Per app:

```bash
pnpm --filter "@bikeapelago/bikepelago-app" run build
pnpm --filter "@bikeapelago/admin-ui" run build
```

## Validation

```bash
pnpm -r --if-present run lint
pnpm --filter "@bikeapelago/bikepelago-app" run test:run
pnpm --filter "@bikeapelago/admin-ui" run tsc
```

Note: no workspace Playwright `test:e2e` script currently; game app tests are Vitest-based.

## Environment Configuration

Set API base URL per app via `.env` files:

- `packages/apps/bikepelago-app/.env`
- `packages/apps/admin-ui/.env`

Example:

```env
VITE_PUBLIC_API_URL=http://localhost:5054
```

Both apps also support `VITE_API_URL` fallback and default to `http://127.0.0.1:5054` in Vite config.

## Docker (Production-style Frontends)

Build runtime images from `frontend/Dockerfile` targets:
- `runtime-frontend`
- `runtime-admin-ui`

At runtime, Nginx templates use:
- `API_PROXY_URL`
- `API_HUBS_URL`

See root compose files for deployment wiring.

## Related Docs

- [Root README](../README.md)
- [Game App README](packages/apps/bikepelago-app/README.md)
- [Admin UI README](packages/apps/admin-ui/README.md)
- [Frontend Claude instructions](CLAUDE.md)
- [Frontend Gemini instructions](GEMINI.md)
- [Frontend Codex instructions](CODEX.md)
