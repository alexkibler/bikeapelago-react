# Bikeapelago AI Guidelines

General instructions for AI agents working in the Bikeapelago monorepo.

## Project Structure
- `api/`: .NET 10 Web API.
- `frontend/`: React SPA with Vite & TypeScript.

## Build and Test Commands
### Root
- **Install All**: `cd frontend && npm install`
- **Build All**: `cd api && dotnet build && cd ../frontend && npm run build`

### Backend (`api/`)
- **Build**: `dotnet build`
- **Run**: `dotnet run`
- **Clean**: `dotnet clean`

### Frontend (`frontend/`)
- **Install**: `npm install`
- **Dev**: `npm run dev`
- **Build**: `npm run build`
- **Lint**: `npm run lint`
- **E2E Tests**: `npm run test:e2e`

## Pending Cleanup

- **Rename working directory**: `avarts/` should be renamed to `bikeapelago/` on disk (`/Volumes/1TB/Repos/avarts` → `/Volumes/1TB/Repos/bikeapelago`). All text references have been updated already. After renaming, also rename the Docker volume `avarts_postgis_data` → `bikeapelago_postgis_data` and remove the `name: avarts_postgis_data` override from `nginx-proxy-manager/docker-compose.yml`.

## Style Guidelines
- **Mono-repo consistency**: Follow existing naming conventions and directory structures.
- **Frontend (React/TS)**: Use functional components, hooks, and Zustand for state.
- **Backend (C#)**: Use repository pattern, dependency injection, and clean architecture.
- **Commit messages**: Use descriptive, imperative messages (e.g., "Add session validation endpoint").
