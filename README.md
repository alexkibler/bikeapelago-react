# Bikeapelago: React & .NET Migration

Bikeapelago is a location-based game integration for [Archipelago](https://archipelago.gg/), originally built with SvelteKit and now migrated to a modern **React** frontend and **.NET 10** Web API backend.

## Project Structure

This monorepo contains both the frontend and backend components of the application:

- `frontend/`: React SPA built with Vite, TypeScript, Tailwind CSS, and Zustand.
- `api/`: .NET 10 Web API providing session management, Archipelago synchronization, and proxy services.

## Prerequisites

- **PostgreSQL**: v14 or later with **PostGIS** extension.
- **Node.js**: v18 or later
- **.NET SDK**: 10.0 or higher

## Getting Started

### 1. Backend (API)

Navigate to the `api` directory and run the application:

```bash
cd api
dotnet restore
dotnet run
```

The API will be available at `http://localhost:5000` (or as configured in `appsettings.json`).

### 2. Frontend

Navigate to the `frontend` directory, install dependencies, and start the development server:

```bash
cd frontend
pnpm install
pnpm run dev
```

The frontend will be available at `http://localhost:5173`.

## Architecture Overview

Bikeapelago uses a decoupled architecture:

- **Frontend**: A React application using **Zustand** for state management and **Tailwind CSS** with **DaisyUI** for styling. It communicates with the .NET API via REST.
- **Backend**: A .NET 10 Web API following a repository pattern. It uses **EF Core** with **PostgreSQL/PostGIS** for persistence and authentication. It also includes an **OsmDiscoveryService** for location-based features.

## Features
- **Archipelago Integration**: Seamlessly connect your real-world activities to your Archipelago multiworld game.
- **Single Player Route Setup**: Upload a GPX or FIT file to create a singleplayer session with nodes distributed evenly along your route. Nodes unlock sequentially as you check them.

## Documentation

- [Architecture Details](ARCHITECTURE.md): Deep dive into the design decisions and patterns.
- [Frontend README](frontend/README.md): Detailed information about the React application.
- [API README](api/README.md): Detailed information about the .NET backend.

## Environment Configuration

The application is configured using environment variables. For local development, create a `.env` file in the project root based on `.env.example`.

### Root `.env` (Backend & Docker)

These variables are primarily used by the .NET API and Docker Compose:

| Variable | Description | Default / Example |
|---|---|---|
| `MAPBOX_API_KEY` | **Required** for routing and node validation. | `pk.eyJ...` |
| `JWT_KEY` | Key for signing authentication tokens. | `openssl rand -base64 32` |
| `DB_USER` | PostgreSQL user for the main application database. | `osm` |
| `DB_PASSWORD` | PostgreSQL password for the main application database. | - |
| `OSM_DISCOVERY_HOST` | Host for the OSM Discovery database (PostGIS). | `localhost` |
| `ADMIN_EMAIL` | Initial admin account email for seeding. | `admin@localhost` |
| `ADMIN_PASSWORD` | Password for the initial admin account. | - |

### Frontend `.env`

Vite reads environment variables from `.env` files in the specific package directories. Create one in each app directory as needed:
- Game App: `frontend/packages/apps/bikepelago-app/.env`
- Admin UI: `frontend/packages/apps/admin-ui/.env`

| Variable | Description | Example |
|---|---|---|
| `VITE_PUBLIC_API_URL` | The base URL for the .NET API. | `http://localhost:5054` |

## Verification

The project includes a comprehensive E2E test suite using **Playwright**, located in the `frontend/tests` directory.

To run the tests:
```bash
cd frontend
npm run test:e2e
```
