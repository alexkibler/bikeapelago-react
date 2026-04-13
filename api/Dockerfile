# ── Stage 1: Build Frontend ──────────────────────────────────────────────────
FROM node:25.9.0-slim AS frontend-build
WORKDIR /src/frontend

# Install pnpm
ENV PNPM_HOME="/pnpm"
ENV PATH="$PNPM_HOME:$PATH"
RUN npm install -g pnpm

# Copy frontend source and workspace configuration
COPY frontend/package.json frontend/pnpm-lock.yaml frontend/pnpm-workspace.yaml ./
COPY frontend/packages ./packages

# Install dependencies and build the main app
RUN pnpm install
RUN pnpm --filter "@bikeapelago/bikeapelago-app" run build

# ── Stage 2: Build API ───────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore dependencies first (layer cache friendly)
COPY api/Bikeapelago.Api.csproj ./api/
RUN dotnet restore api/Bikeapelago.Api.csproj

# Copy API source
COPY api/ ./api/

# Copy frontend build output to API's wwwroot
COPY --from=frontend-build /src/frontend/packages/apps/bikepelago-app/dist/ ./api/wwwroot/

# Publish the API
RUN dotnet publish api/Bikeapelago.Api.csproj -c Release -o /app/publish

# ── Stage 3: Runtime ─────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish ./

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:5054

EXPOSE 5054

ENTRYPOINT ["dotnet", "Bikeapelago.Api.dll"]
