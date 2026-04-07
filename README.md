# Bikeapelago: React & .NET Migration

This repository contains the port of the original SvelteKit/Node.js application to a modern **React** frontend and a **.NET 8** Web API backend.

## Project Structure

- `frontend/`: React application built with Vite, Tailwind CSS, DaisyUI, and Zustand.
- `api/`: .NET 8 Web API providing session management, Archipelago synchronization, and proxy services.

## Prerequisites

- **Node.js**: v18+
- **.NET SDK**: 8.0 or higher
- **Archipelago**: An active Archipelago multiworld server (for game features).

## Local Development

### Frontend

1. Navigate to the `frontend` directory:
   ```bash
   cd frontend
   ```
2. Install dependencies:
   ```bash
   npm install
   ```
3. Start the dev server:
   ```bash
   npm run dev
   ```

### Backend

1. Navigate to the `api` directory:
   ```bash
   cd api
   ```
2. Restore and run the API:
   ```bash
   dotnet run
   ```

## Key Technologies

- **Frontend**: React, React Router, react-leaflet, Zustand, Tailwind CSS, DaisyUI.
- **Backend**: .NET 8, ASP.NET Core, Entity Framework Core (SQLite).
- **Game Engine**: Archipelago integration for item/location tracking.

## Verification & Progress

The migration has been verified using a full E2E test suite. Below is the visual progress of the React/.NET implementation:

| Login | Dashboard | New Session |
|---|---|---|
| ![Login](file:///Volumes/1TB/Repos/avarts/bikeapelago-src/react-dotnet-migration/docs/screenshots/01-login.png) | ![Dashboard](file:///Volumes/1TB/Repos/avarts/bikeapelago-src/react-dotnet-migration/docs/screenshots/02-home.png) | ![New Game](file:///Volumes/1TB/Repos/avarts/bikeapelago-src/react-dotnet-migration/docs/screenshots/03-new-game.png) |

| Setup | Game View | Rejoin |
|---|---|---|
| ![Setup](file:///Volumes/1TB/Repos/avarts/bikeapelago-src/react-dotnet-migration/docs/screenshots/04-setup.png) | ![Game View](file:///Volumes/1TB/Repos/avarts/bikeapelago-src/react-dotnet-migration/docs/screenshots/05-game-view.png) | ![Rejoin](file:///Volumes/1TB/Repos/avarts/bikeapelago-src/react-dotnet-migration/docs/screenshots/07-rejoined.png) |

### Current Status: 🟢 Functional Parity Reach
- **Frontend**: Full React port completed with Tailwind CSS and DaisyUI.
- **Backend**: .NET 8 API with Repository abstraction for PocketBase.
- **E2E**: Full login and session rejoining flow verified via Playwright.

---

## Deployment

The application is designed to be easily containerized:
- **Frontend**: Shipped as a static site (Vite build).
- **Backend**: Deployed as a .NET container or standalone executable.
