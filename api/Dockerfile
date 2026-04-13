# ── Stage 1: Build ───────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Restore dependencies first (layer cache friendly)
COPY api/Bikeapelago.Api.csproj ./api/
RUN dotnet restore api/Bikeapelago.Api.csproj

# Copy API source
COPY api/ ./api/

# Publish the API
RUN dotnet publish api/Bikeapelago.Api.csproj -c Release -o /app/publish

# ── Stage 2: Runtime ─────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

# Install dependencies for PostGIS/NetTopologySuite if needed
RUN apk add --no-cache icu-libs krb5-libs libgcc libintl libssl3 libstdc++ zlib

COPY --from=build /app/publish ./

# Use 8080 to match our docker-compose and common non-root conventions
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "Bikeapelago.Api.dll"]
