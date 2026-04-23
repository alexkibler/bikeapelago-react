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

## ⛔ CRITICAL SAFETY: DATABASE PROTECTION
- **ZERO DATABASE WRITES WITHOUT APPROVAL**: You are FORBIDDEN from executing any command that modifies a database schema or data without explicit, per-command approval from the user.
- **Commands Affected**: `psql`, `docker exec ... psql`, `dotnet ef database update`, or any script that performs `INSERT`, `UPDATE`, `DELETE`, `DROP`, `CREATE`, or `ALTER`.
- **Mandatory Workflow**: 
  1. Create an artifact/description of the proposed changes.
  2. Wait for the user to reply with "Approved".
  3. Only then execute the commands.
- **NO EXCEPTIONS**: Even to fix "broken" states or missing tables discovered during a task.
