# Bikeapelago Gemini Instructions

Global guidance for Gemini in this repository.

## Safety

- Do not run direct SQL write commands.
- Use EF migrations for schema changes.
- Request approval before any irreversible data operation.
- For frontend tool/API calls, prefer RTK Query wherever the app is already wired for RTK. Use existing shared request helpers or direct `fetch` only when RTK is not available, the call is intentionally one-off, or the surrounding code already has a narrower local pattern.

## Build and Test

### API

```bash
cd api
dotnet build
dotnet run
cd ../
dotnet test api.Tests/Bikeapelago.Api.Tests.csproj
```

### Frontend

```bash
cd frontend
pnpm install
pnpm -r --if-present run build
pnpm -r --if-present run lint
pnpm --filter "@bikeapelago/bikepelago-app" run test:run
pnpm --filter "@bikeapelago/admin-ui" run tsc
```

## Working Rules

- Make surgical edits to existing files.
- Keep docs synchronized with scripts and config.
- Prefer pnpm workspace commands over npm for frontend packages.
