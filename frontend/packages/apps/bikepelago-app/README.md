# Bikeapelago: React Frontend

The Bikeapelago frontend is a high-performance React application designed for real-time interaction with the .NET backend and the [Archipelago](https://archipelago.gg/) network.

## Tech Stack

- **React 18**: Component-based UI library.
- **Vite**: Ultra-fast build tool and development server.
- **Zustand**: Minimalist state management for game and auth state.
- **Tailwind CSS + DaisyUI**: Modern, utility-first styling with accessible component themes.
- **React Leaflet**: Interactive map rendering and location tracking.
- **Playwright**: Comprehensive E2E testing framework.

## Project Structure

- `src/components/`: Reusable UI components (Game views, Layouts).
- `src/hooks/`: Custom React hooks for business logic and data fetching.
- `src/lib/`: Core utilities for Archipelago, Geocoding, and GraphHopper.
- `src/pages/`: Main application routes (Home, Login, GameView).
- `src/store/`: Zustand state stores for centralized application state.
- `tests/e2e/`: Full suite of Playwright tests verifying critical user flows.

## Getting Started

### Installation
```bash
npm install
```

### Run Dev Server
```bash
npm run dev
```

### Run E2E Tests
```bash
npm run test:e2e
```

## Configuration

The application is configured via environment variables in `.env` files.

### Key Variables
- `VITE_API_URL`: The base URL for the .NET API.

## Deployment

The frontend is built as a static SPA:
```bash
npm run build
```
The output will be in the `dist/` directory, which can be served by Nginx or integrated into the .NET API's static file hosting.
