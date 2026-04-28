# Admin UI Gemini Instructions

Guidance for Gemini in `packages/apps/admin-ui`.

## Build and Validation

Run from `frontend/`:

```bash
pnpm --filter "@bikeapelago/admin-ui" run build
pnpm --filter "@bikeapelago/admin-ui" run tsc
```

## Development

```bash
pnpm --filter "@bikeapelago/admin-ui" run dev
```

## Rules

- Keep edits focused and reversible.
- Preserve existing architecture and API contracts.
- Prefer RTK Query for new tool/API calls wherever possible. If the touched admin area still uses its existing query/client pattern, keep changes consistent and avoid adding one-off request helpers.
- Keep README and instruction docs up to date.
