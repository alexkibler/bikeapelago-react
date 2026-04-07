# Bikeapelago Gemini Guidelines

Instructions for Gemini CLI agent working in this workspace.

## Build and Test Commands
- **Backend**: `cd api && dotnet build`
- **Frontend Install**: `cd frontend && npm install`
- **Frontend Build**: `cd frontend && npm run build`
- **E2E Tests**: `cd frontend && npm run test:e2e`

## Code Standards
- **Surgical Updates**: Use the `replace` tool for precise edits to existing code.
- **Validation**: Always run tests (e.g., `npm run test:e2e`) after making changes.
- **Clean Code**: Adhere to the clean architecture patterns in the API and functional patterns in the frontend.
- **Documentation**: Keep README and architecture files up-to-date with any major changes.
