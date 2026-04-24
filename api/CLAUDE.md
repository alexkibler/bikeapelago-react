# API Claude Instructions

Guidance for Claude working inside `api/`.

## Safety

- Do not execute direct SQL mutations manually.
- Use EF migrations for schema changes.

## Commands

From `api/`:

```bash
dotnet build
dotnet run
dotnet clean
dotnet format
```

From repo root:

```bash
dotnet test api.Tests/Bikeapelago.Api.Tests.csproj
```

## Implementation Guidelines

- Keep controllers thin.
- Put business logic in services.
- Keep data access in repositories.
- Prefer async APIs for I/O work.
- Preserve nullable reference safety.

## Documentation Rule

If API behavior, endpoints, or config change, update:
- `api/README.md`
- `api/API_ENDPOINTS.md` (if endpoint surface changes)
- root `README.md` when setup/deployment changes
