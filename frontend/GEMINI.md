# Frontend Gemini Instructions

Guidance for Gemini working inside `frontend/`.

## Build and Validation

```bash
pnpm install
pnpm -r --if-present run build
pnpm -r --if-present run lint
pnpm --filter "@bikeapelago/bikepelago-app" run test:run
pnpm --filter "@bikeapelago/admin-ui" run tsc
```

## Development

```bash
pnpm --filter "@bikeapelago/bikepelago-app" run dev
pnpm --filter "@bikeapelago/admin-ui" run dev
```

## Rules

- Prefer focused edits over broad rewrites.
- Keep TypeScript strictness intact.
- Keep docs aligned with actual scripts in `package.json`.
