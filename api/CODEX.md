# API Codex Instructions

Guidance for Codex agents working in `api/`.

## Core Constraints

- Avoid direct SQL mutations; use EF migrations.
- Keep architecture boundaries intact (controller -> service -> repository).

## Commands

From `api/`:

```bash
dotnet build
dotnet run
dotnet format
```

From repo root:

```bash
dotnet test api.Tests/Bikeapelago.Api.Tests.csproj
```

## Notes

- API dev URL: `http://localhost:5054`
- Startup attempts to apply migrations automatically.
- SignalR hub route: `/hubs/archipelago`

## Documentation Contract

When changing API behavior, update `api/README.md` and `api/API_ENDPOINTS.md` as needed.
