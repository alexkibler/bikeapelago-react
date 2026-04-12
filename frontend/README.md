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

Frontend configuration (primarily `VITE_PUBLIC_API_URL`) is managed via `.env` files. See the [Environment Configuration](../README.md#environment-configuration) section in the root README for details.

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

### Docker Testing (Production-like)

Test the app in a Docker container without modifying local machine state. The container serves the built React app via Nginx and proxies API calls to the backend.

**Build the Docker image:**
```bash
docker build -f frontend/Dockerfile \
  -t bikeapelago-frontend:latest \
  --target runtime-frontend \
  frontend
```

**Run against production API:**
```bash
docker run -d --name frontend-test -p 8080:80 \
  -e API_PROXY_URL=https://bikeapelago.alexkibler.com/api/ \
  -e API_HUBS_URL=https://bikeapelago.alexkibler.com/hubs/ \
  bikeapelago-frontend:latest
```

Then open `http://localhost:8080` and test:
- Frontend loads ✓
- Login works (API calls to production) ✓
- Game page functions (WebSocket connections work) ✓

**Run against local API (if available):**
```bash
docker run -d --name frontend-test -p 8080:80 \
  -e API_PROXY_URL=http://host.docker.internal:8080/api/ \
  -e API_HUBS_URL=http://host.docker.internal:8080/hubs/ \
  bikeapelago-frontend:latest
```

**Stop the container:**
```bash
docker kill frontend-test
docker rm frontend-test
```

---

### Local Development (Vite dev server)

**Debug the game app locally with hot reload:**
```bash
pnpm install
pnpm --filter "@bikeapelago/bikeapelago-app" run dev
```
Opens at `http://localhost:5173` with live reloading on file changes.

**Debug the admin UI locally:**
```bash
pnpm install
pnpm --filter "@bikeapelago/admin-ui" run dev
```
Opens at `http://localhost:5174` (or see terminal for exact port).

**Point dev server to production API:**

Edit `.env` in the frontend root:
```env
VITE_PUBLIC_API_URL=https://bikeapelago.alexkibler.com
```

Then restart the dev server. All API calls go to production while you iterate on frontend code locally.

---

### Docker Testing - Admin UI

**Build the admin UI Docker image:**
```bash
docker build -f frontend/Dockerfile \
  -t bikeapelago-admin-ui:latest \
  --target runtime-admin-ui \
  frontend
```

**Run the admin UI against production API:**
```bash
docker run -d --name admin-test -p 8081:80 \
  -e API_PROXY_URL=https://bikeapelago.alexkibler.com/api/ \
  -e API_HUBS_URL=https://bikeapelago.alexkibler.com/hubs/ \
  bikeapelago-admin-ui:latest
```

Open `http://localhost:8081` and test the admin dashboard.

**Stop the container:**
```bash
docker kill admin-test
docker rm admin-test
```

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
