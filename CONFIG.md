# Configuration Guide

This document explains how configuration works in Bikeapelago for different environments.

## Overview

Configuration follows a layered approach:
1. **Base config** (appsettings.json) - checked into repo, safe defaults
2. **Environment overrides** (appsettings.{Environment}.json) - environment-specific, checked in
3. **Secrets** (environment variables / .env) - sensitive data, NOT checked in

## Local Development

### Setup

1. **Copy `.env.example` to `.env`** (Git-ignored file for your machine)
   ```bash
   cp .env.example .env
   ```

2. **Fill in required values in `.env`:**
   - `POSTGIS_CONNECTION_STRING` - your local database
   - `MAPBOX_API_KEY` - get from Mapbox console
   - `JWT_KEY` - generate with `openssl rand -base64 32`
   - `ADMIN_EMAIL` / `ADMIN_PASSWORD` - initial admin account

3. **Start the services:**
   ```bash
   docker compose --profile api up
   ```

### Configuration File Precedence

In local development (.NET reads from left to right, right wins):

```
appsettings.json
  ← appsettings.Development.json
    ← Environment variables (from .env)
```

**Example: `MAPBOX_API_KEY`**
- In `appsettings.json`: `"ApiKey": "sk.eyJ..."`(placeholder - will be overridden)
- From `.env`: `MAPBOX_API_KEY=sk.your_actual_key_here`
- **Final value: Uses `.env` value**

## Docker / Production Deployment

### What the Container Reads

The API container **ONLY reads environment variables** (no .env file). Configuration flows like this:

```
appsettings.json
  ← appsettings.Production.json (if in Production environment)
    ← Environment variables (from docker-compose.yml or Kubernetes)
```

### Example: docker-compose.yml

```yaml
services:
  api:
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__PostGis=Host=postgis;Port=5432;...
      - Mapbox__ApiKey=${MAPBOX_API_KEY}
      - Jwt__Key=${JWT_KEY}
      - Admin__Email=${ADMIN_EMAIL}
      - Admin__Password=${ADMIN_PASSWORD}
```

**Note:** The double underscore `__` in env vars becomes `.` in appsettings (e.g., `Mapbox__ApiKey` → `Mapbox.ApiKey`)

## Current Configuration Values

### Connection Strings

| Setting | Local | Docker |
|---------|-------|--------|
| `PostGis` | localhost:5432 | `postgis:5432` (Docker DNS) |
| Source | `appsettings.json` | Overridden by env var |

### Mapbox

| Setting | Value | Source |
|---------|-------|--------|
| `Mapbox:ApiKey` | In `appsettings.json` | Overridden by `.env` (local) or env var (Docker) |
| Sensitive? | **YES** | Should never be in source control |

### JWT & Admin Credentials

| Setting | Local | Docker |
|---------|-------|--------|
| `Jwt:Key` | Generated at startup if missing | Must be provided via env var |
| `Admin:Email/Password` | From `appsettings.json` defaults | Overridden by env vars |
| Sensitive? | **YES** | Never in source control |

### Elevation Data

Elevation caching has been **removed entirely**. No backend elevation processing - frontend handles it.

- Removed services: `GridCacheJobProcessor`, `RegionalElevationService`, `GridCacheService`
- Removed database: `osm_discovery` (no longer used)
- Frontend can query elevation from external APIs like Mapbox/Tilequery

## .env.example vs appsettings.json

| File | Source Control? | Purpose |
|------|-----------------|---------|
| `.env.example` | ✅ Yes | Template showing what env vars are available |
| `.env` | ❌ NO (.gitignored) | Your actual secrets for local development |
| `appsettings.json` | ✅ Yes | Base config + safe defaults |
| `appsettings.Production.json` | ✅ Yes | Production-specific (non-secret) overrides |

## Quick Reference

### To Run Locally

```bash
# 1. Copy template
cp .env.example .env

# 2. Edit .env with your actual API keys
nano .env

# 3. Start services
docker compose --profile api up
```

### To Deploy to Docker

```bash
# Set env vars (example with GitHub Actions)
export MAPBOX_API_KEY=sk.your_key
export JWT_KEY=$(openssl rand -base64 32)
export ADMIN_PASSWORD=secure_password

# Start
docker compose --profile api up -d
```

### To Check What's Being Used

```bash
# In the API container
docker compose exec api printenv | grep -i mapbox
docker compose exec api printenv | grep -i postgis
```
