# Bikeapelago: API Guidelines (Claude)

Instructions for Claude working on the .NET API.

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
