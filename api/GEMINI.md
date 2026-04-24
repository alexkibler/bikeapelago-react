# API Gemini Instructions

Guidance for Gemini working inside `api/`.

## Safety

- No direct SQL write operations.
- Use EF Core migration workflow for schema changes.

## Build and Test

From `api/`:

```bash
dotnet build
dotnet run
```

From repo root:

```bash
dotnet test api.Tests/Bikeapelago.Api.Tests.csproj
```

## Working Rules

- Make targeted edits.
- Preserve repository/service layering.
- Keep docs updated when endpoints/config/scripts change.
