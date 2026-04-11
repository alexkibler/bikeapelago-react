# Bikeapelago: React Frontend (pnpm Workspaces)

High-performance React SPA built with **Vite**, **React 19**, and **pnpm workspaces** for monorepo package management.

## Tech Stack

- **React 19**: Component-based UI
- **TypeScript**: Strict typing
- **Vite**: Ultra-fast build tool & dev server
- **pnpm**: Fast, efficient monorepo package manager
- **Zustand**: Minimalist state management (game, auth, map stores)
- **Tailwind CSS + DaisyUI**: Utility-first styling with accessible components
- **React Leaflet**: Interactive map rendering and location tracking
- **Playwright**: E2E testing

## Monorepo Structure

```
frontend/
├── package.json                 # Root pnpm workspaces config
├── pnpm-workspace.yaml
├── packages/
│   ├── apps/
│   │   ├── bikepelago-app/     # Main game application
│   │   └── admin-ui/            # Admin dashboard
│   └── shared/                  # (future) Shared utilities, hooks, types
```

## Getting Started

### Prerequisites

- **Node.js** 18+ (check with `node -v`)
- **pnpm** 10+ (`npm install -g pnpm` if needed)
- **.NET API** running on `http://localhost:5054` (see `/api/README.md`)
- **PostGIS** running in Docker (`docker compose up postgis`)

### Installation

```bash
# Install all workspace dependencies
pnpm install
```

### Development

**Run the main game app:**
```bash
pnpm --filter "@bikeapelago/bikeapelago-app" run dev
```

**Run admin UI:**
```bash
pnpm --filter "@bikeapelago/admin-ui" run dev
```

**Run all:**
```bash
pnpm -r run dev
```

Then open:
- Game: `http://localhost:5173`
- Admin: `http://localhost:5174` (or check terminal output)

### Building

```bash
# Build all packages
pnpm -r run build

# Build specific app
pnpm --filter "@bikeapelago/bikeapelago-app" run build
```

### Testing

```bash
# Run E2E tests (Playwright)
pnpm --filter "@bikeapelago/bikeapelago-app" run test:e2e

# Run specific test
pnpm --filter "@bikeapelago/bikeapelago-app" run test:e2e tests/e2e/game.spec.ts
```

### Linting

```bash
# Lint all packages
pnpm -r run lint

# Fix lint issues
pnpm -r run lint -- --fix
```

## Project Structure (bikepelago-app)

- `src/components/` — Reusable UI components (game views, layouts, forms)
- `src/hooks/` — Custom React hooks (API calls, state management, geolocation)
- `src/lib/` — Core utilities (API client, geocoding, Mapbox integration)
- `src/pages/` — Main application routes (Home, Login, GameView)
- `src/store/` — Zustand state stores (userStore, gameStore, mapStore)
- `src/types/` — TypeScript type definitions
- `tests/e2e/` — Playwright end-to-end tests

## Configuration

### Environment Variables

Create a `.env` at the monorepo root:

```
VITE_PUBLIC_API_URL=http://localhost:5054
VITE_PUBLIC_DB_URL=https://pb.bikeapelago.alexkibler.com
```

### Vite Config

- `vite.config.ts` — Main app Vite configuration
- `playwright.config.ts` — E2E test configuration

## Routing Architecture

The app uses **React Router v7** for SPA routing:

- `/` — Login / Home
- `/game/:sessionId` — Game view (main interactive map)
- Protected routes require JWT authentication (token stored in Zustand)

## State Management (Zustand)

Three stores manage application state:

1. **userStore** — Authentication, user profile
2. **gameStore** — Session data, Archipelago state, location tracking
3. **mapStore** — Leaflet map state, routing params, active waypoints

Stores are persistent (localStorage) and subscribe directly to React components via hooks.

## API Integration

The frontend communicates with the `.NET API` at `/api`:

- **Sessions** — Create, list, generate nodes
- **Authentication** — Register, login (JWT)
- **Nodes** — Fetch, update state (Hidden/Available/Checked)
- **Discovery** — Validate coordinates via Mapbox
- **Routing** — Optimize routes to available nodes

API calls use `axios` via `src/lib/apiClient.ts`.

## Mapbox Integration

The app uses **Mapbox APIs** for:

- **Route Display** — Visualizing optimized routes on the map
- **Node Validation** — Snapping coordinates to roads
- **Route Optimization** — Finding efficient visit order through nodes

Mapbox key is configured in the backend (`.env` `MAPBOX_API_KEY`).

## Deployment

Built as a **static SPA**:

```bash
pnpm --filter "@bikeapelago/bikeapelago-app" run build
```

Output goes to `dist/` — ready to serve with any static host (Netlify, Vercel, nginx, etc).

Docker image: See `frontend/Dockerfile.dev` for local dev container setup.

## Common Commands

```bash
# Install a new package in a specific workspace
pnpm --filter "@bikeapelago/bikeapelago-app" add lodash

# Remove a package
pnpm --filter "@bikeapelago/bikeapelago-app" remove lodash

# Run a script in all workspaces
pnpm -r run <script>

# Check pnpm version
pnpm -v

# Show workspace tree
pnpm list --depth -1
```

## Troubleshooting

**Port 5173 already in use?**
```bash
pnpm --filter "@bikeapelago/bikeapelago-app" run dev -- --port 5174
```

**API connection refused?**
- Ensure `.NET API` is running on `http://localhost:5054`
- Check `VITE_PUBLIC_API_URL` in `.env`

**Playwright tests fail?**
```bash
pnpm --filter "@bikeapelago/bikeapelago-app" exec playwright install
```

## Contributing

See parent repo `/CLAUDE.md` for general guidelines. Frontend-specific:

- Use functional components with TypeScript
- Keep components small and focused
- Abstract logic into custom hooks
- Use Zustand for shared state
- Add E2E tests for critical user flows
- Follow Tailwind + DaisyUI for styling
