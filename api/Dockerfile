# ── Stage 1: Build API ───────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore dependencies first (layer cache friendly)
COPY api/Bikeapelago.Api.csproj ./api/
RUN dotnet restore api/Bikeapelago.Api.csproj

# Copy API source
COPY api/ ./api/

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
