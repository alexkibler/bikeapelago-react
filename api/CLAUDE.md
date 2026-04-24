# Bikeapelago: API Guidelines (Claude)

Instructions for Claude working on the .NET API.

> [!IMPORTANT]
> **DATABASE SAFETY**: NEVER execute DDL or DML directly against the database (no `psql -c`, no `docker exec ... psql`, no raw SQL run outside of migrations). All schema changes go through `dotnet ef migrations add` + `dotnet ef database update`. If a migration needs raw SQL, use `migrationBuilder.Sql()` inside the migration file. No exceptions.

## Build and Test Commands
- **Build**: `dotnet build`
- **Run**: `dotnet run`
- **Clean**: `dotnet clean`
- **Format**: `dotnet format`
- **Update Database**: `dotnet ef database update`

## Style Guidelines
- **Architecture**: Adhere to the Repository Pattern and Dependency Injection.
- **Naming**: Use PascalCase for classes, methods, and properties. Use camelCase for local variables and parameters.
- **Async**: Favor `async/await` for all I/O-bound operations.
- **Safety**: Use C# 10 nullable reference types and modern language features.
- **Controllers**: Keep controllers thin; delegate business logic to services.
