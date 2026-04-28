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
- Prefer RTK Query for new frontend tool/API calls wherever RTK is available in the target app.
- Reuse existing shared request utilities or local fetch/query patterns when adding RTK would be out of scope for the change, but avoid introducing new ad hoc request code when an RTK endpoint can cover it.
- Keep docs aligned with actual scripts in `package.json`.
