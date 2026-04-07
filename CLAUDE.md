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

## Style Guidelines
- **Mono-repo consistency**: Follow existing naming conventions and directory structures.
- **Frontend (React/TS)**: Use functional components, hooks, and Zustand for state.
- **Backend (C#)**: Use repository pattern, dependency injection, and clean architecture.
- **Commit messages**: Use descriptive, imperative messages (e.g., "Add session validation endpoint").
