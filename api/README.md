# Bikeapelago: .NET 10 API Backend

The backend for Bikeapelago is built with ASP.NET Core 10, providing a robust, scalable, and type-safe layer for game session management, authentication, and external service integration.

## Key Technologies

- **ASP.NET Core 10.0**: High-performance web framework.
- **Entity Framework Core**: Used with **PostgreSQL/PostGIS** for persistence and spatial queries.
- **Identity Server / JWT**: Handles user authentication and session security.
- **Swagger/OpenAPI**: API documentation and interactive testing at `/swagger`.

## Project Structure

- `Controllers/`: HTTP endpoints for Auth, Nodes, and Sessions.
- `Data/`: DB context and migration-related logic.
- `Models/`: DTOs, request models, and internal entities.
- **Repositories**: Abstracted data access layer with PostgreSQL and Mock implementations.
- `Services/`: Core business logic (OSM discovery, node generation, etc.).

## Configuration

Settings are managed in `appsettings.json` and `appsettings.Development.json`.

### Key Settings
- `ConnectionStrings:DefaultConnection`: PostgreSQL connection string.
- `AllowedOrigins`: Array of CORS-allowed URLs (e.g., `["http://localhost:5173"]`).
- `USE_MOCK_AUTH`: If set to `true`, the API uses in-memory mock repositories instead of the database.

## Commands

### Run the API
```bash
dotnet run
```

### Build the API
```bash
dotnet build
```

### Database Management
Ensure you have the EF Core tools installed:
```bash
dotnet ef database update
```

## API Endpoints

- `/api/auth`: Login and session validation.
- `/api/nodes`: Geocoding and map node discovery (PostGIS-backed).
- `/api/sessions`: Creation and management of Archipelago game sessions.
