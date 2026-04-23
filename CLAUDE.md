# Bikeapelago AI Guidelines

General instructions for AI agents working in the Bikeapelago monorepo.

> [!IMPORTANT]
> **DATABASE SAFETY**: NEVER perform database writes (DDL/DML) without explicit approval. See `GEMINI.md` for full policy.

## Project Structure
- `api/`: .NET 10 Web API.
- `frontend/`: React SPA with Vite & TypeScript.

## Build and Test Commands
### Backend (`api/`)
- **Build**: `dotnet build`
- **Run**: `dotnet run`
- **Clean**: `dotnet clean`

### Frontend (`frontend/`) — pnpm workspaces
- **Install**: `pnpm install`
- **Dev (game app)**: `pnpm --filter "@bikeapelago/bikeapelago-app" run dev`
- **Dev (admin UI)**: `pnpm --filter "@bikeapelago/admin-ui" run dev`
- **Build all**: `pnpm -r run build`
- **Lint**: `pnpm -r run lint`
- **E2E Tests**: `pnpm --filter "@bikeapelago/bikeapelago-app" run test:e2e`

## Deployment (OrbStack)

Images are built by CI on push to main and pushed to GHCR. Pull and restart after merging:

```bash
docker compose -f docker-compose.deploy.yml pull
docker compose -f docker-compose.deploy.yml up -d --force-recreate
```

### Architecture

- `bikeapelago-react` (port `8192:80`): nginx serving the frontend SPA. Proxies `/api` and `/hubs` to the API container over the internal Docker network.
- `bikeapelago-api` (port `5054:5054`): .NET API only — does **not** serve the frontend.
- `bikeapelago-admin` (port `8183:80`): nginx serving the admin UI.
- nginx-proxy-manager routes `bikeapelago.alexkibler.com` → `host.docker.internal:8192`.
- The API has no public-facing URL; all external traffic reaches it through the frontend nginx container.

### Local Frontend Dev (hot reload against running API container)

The API container exposes port 5054 to the host. Set in `frontend/packages/apps/bikepelago-app/.env`:

```env
VITE_PUBLIC_API_URL=http://localhost:5054
```

Then run:

```bash
pnpm --filter "@bikeapelago/bikeapelago-app" run dev
```

## Pending Cleanup

- **Rename working directory**: `avarts/` should be renamed to `bikeapelago/` on disk (`/Volumes/1TB/Repos/avarts` → `/Volumes/1TB/Repos/bikeapelago`). All text references have been updated already. After renaming, also rename the Docker volume `avarts_postgis_data` → `bikeapelago_postgis_data` and remove the `name: avarts_postgis_data` override from `nginx-proxy-manager/docker-compose.yml`.

## Style Guidelines
- **Mono-repo consistency**: Follow existing naming conventions and directory structures.
- **Frontend (React/TS)**: Use functional components, hooks, and Zustand for state.
- **Backend (C#)**: Use repository pattern, dependency injection, and clean architecture.
- **Commit messages**: Use descriptive, imperative messages (e.g., "Add session validation endpoint").

<!-- rtk-instructions v2 -->
# RTK (Rust Token Killer) - Token-Optimized Commands

## Golden Rule

**Always prefix commands with `rtk`**. If RTK has a dedicated filter, it uses it. If not, it passes through unchanged. This means RTK is always safe to use.

**Important**: Even in command chains with `&&`, use `rtk`:
```bash
# ❌ Wrong
git add . && git commit -m "msg" && git push

# ✅ Correct
rtk git add . && rtk git commit -m "msg" && rtk git push
```

## RTK Commands by Workflow

### Build & Compile (80-90% savings)
```bash
rtk cargo build         # Cargo build output
rtk cargo check         # Cargo check output
rtk cargo clippy        # Clippy warnings grouped by file (80%)
rtk tsc                 # TypeScript errors grouped by file/code (83%)
rtk lint                # ESLint/Biome violations grouped (84%)
rtk prettier --check    # Files needing format only (70%)
rtk next build          # Next.js build with route metrics (87%)
```

### Test (90-99% savings)
```bash
rtk cargo test          # Cargo test failures only (90%)
rtk vitest run          # Vitest failures only (99.5%)
rtk playwright test     # Playwright failures only (94%)
rtk test <cmd>          # Generic test wrapper - failures only
```

### Git (59-80% savings)
```bash
rtk git status          # Compact status
rtk git log             # Compact log (works with all git flags)
rtk git diff            # Compact diff (80%)
rtk git show            # Compact show (80%)
rtk git add             # Ultra-compact confirmations (59%)
rtk git commit          # Ultra-compact confirmations (59%)
rtk git push            # Ultra-compact confirmations
rtk git pull            # Ultra-compact confirmations
rtk git branch          # Compact branch list
rtk git fetch           # Compact fetch
rtk git stash           # Compact stash
rtk git worktree        # Compact worktree
```

Note: Git passthrough works for ALL subcommands, even those not explicitly listed.

### GitHub (26-87% savings)
```bash
rtk gh pr view <num>    # Compact PR view (87%)
rtk gh pr checks        # Compact PR checks (79%)
rtk gh run list         # Compact workflow runs (82%)
rtk gh issue list       # Compact issue list (80%)
rtk gh api              # Compact API responses (26%)
```

### JavaScript/TypeScript Tooling (70-90% savings)
```bash
rtk pnpm list           # Compact dependency tree (70%)
rtk pnpm outdated       # Compact outdated packages (80%)
rtk pnpm install        # Compact install output (90%)
rtk npm run <script>    # Compact npm script output
rtk npx <cmd>           # Compact npx command output
rtk prisma              # Prisma without ASCII art (88%)
```

### Files & Search (60-75% savings)
```bash
rtk ls <path>           # Tree format, compact (65%)
rtk read <file>         # Code reading with filtering (60%)
rtk grep <pattern>      # Search grouped by file (75%)
rtk find <pattern>      # Find grouped by directory (70%)
```

### Analysis & Debug (70-90% savings)
```bash
rtk err <cmd>           # Filter errors only from any command
rtk log <file>          # Deduplicated logs with counts
rtk json <file>         # JSON structure without values
rtk deps                # Dependency overview
rtk env                 # Environment variables compact
rtk summary <cmd>       # Smart summary of command output
rtk diff                # Ultra-compact diffs
```

### Infrastructure (85% savings)
```bash
rtk docker ps           # Compact container list
rtk docker images       # Compact image list
rtk docker logs <c>     # Deduplicated logs
rtk kubectl get         # Compact resource list
rtk kubectl logs        # Deduplicated pod logs
```

### Network (65-70% savings)
```bash
rtk curl <url>          # Compact HTTP responses (70%)
rtk wget <url>          # Compact download output (65%)
```

### Meta Commands
```bash
rtk gain                # View token savings statistics
rtk gain --history      # View command history with savings
rtk discover            # Analyze Claude Code sessions for missed RTK usage
rtk proxy <cmd>         # Run command without filtering (for debugging)
rtk init                # Add RTK instructions to CLAUDE.md
rtk init --global       # Add RTK to ~/.claude/CLAUDE.md
```

## Token Savings Overview

| Category | Commands | Typical Savings |
|----------|----------|-----------------|
| Tests | vitest, playwright, cargo test | 90-99% |
| Build | next, tsc, lint, prettier | 70-87% |
| Git | status, log, diff, add, commit | 59-80% |
| GitHub | gh pr, gh run, gh issue | 26-87% |
| Package Managers | pnpm, npm, npx | 70-90% |
| Files | ls, read, grep, find | 60-75% |
| Infrastructure | docker, kubectl | 85% |
| Network | curl, wget | 65-70% |

Overall average: **60-90% token reduction** on common development operations.
<!-- /rtk-instructions -->