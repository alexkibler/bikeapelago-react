# OSM Discovery Migration Progress

## ✅ Accomplished
- **Dependency Injection**: Added `Npgsql`, `OsmSharp`, and `NetTopologySuite`.
- **Interface Definition**: Created `IOsmDiscoveryService` to standardize node discovery and validation.
- **Provider Implementations**:
    - **PostGIS**: Lifted the logic from `osm-random-node-api`.
    - **PBF**: Implemented a two-pass streaming parser using `OsmSharp`.
    - **Overpass**: Implemented a fallback HTTP provider.
    - **Mock**: Maintained testing support.
- **Service Coordinator**: Implemented logic to switch providers based on `OsmDiscovery:PbfPath` or `PostGis` connection strings.
- **Restore Program.cs**: Repaired the broken service registrations and cleaned up redundant registrations.
- **NodesController Update**: Added the `POST /api/discovery/validate-nodes` endpoint.
- **Verify NodeGenerationService**: Switched `NodeGenerationService` to use interface-driven DI for the OSM Discovery service.
- **Configuration Check**: Added the `OsmDiscovery:PbfPath` configuration to `appsettings.json`.

## 🛠 To Do
*(Migration complete)*
