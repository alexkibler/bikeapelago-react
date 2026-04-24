# Bikeapelago Codex Instructions

Global guidance for Codex-based agents in this repository.

## Core Rules

- Never execute raw SQL mutations directly against project databases.
- Schema changes must go through EF Core migrations.
- Keep changes scoped and avoid broad refactors unless requested.

## Project Map

- `api/` - ASP.NET Core API (`net10.0`)
- `api.Tests/` - xUnit tests
- `frontend/` - pnpm workspace
- `frontend/packages/apps/bikepelago-app/` - player-facing app
- `frontend/packages/apps/admin-ui/` - admin app

## Commands

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

## Documentation Contract

When behavior changes, update docs in the same PR:
- root `README.md`
- `api/README.md`
- `frontend/README.md`
- package-level docs in `frontend/packages/apps/*`
