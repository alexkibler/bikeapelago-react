# Bikeapelago Development TODOs

## Phase 1: Core Service Refactoring (In Progress)
- [x] Consolidate `NodesController` into `SessionsController`
- [x] Update frontend to use new session-based node endpoints

## Phase 2: Enhanced Validation (Completed)
- [x] Implement `SessionValidator` class to centralize business rules
- [x] Migrate inline validation from `SessionsController` to `SessionValidator`
- [x] Add TODO/placeholder for .fit vs .gpx timestamp validation
- [x] Unit test `SessionValidator` independently

## Phase 3: Technical Debt & Modularity (Completed)
- [x] Extract interfaces for all core services:
    - [x] `IArchipelagoService`
    - [x] `IFitAnalysisService`
    - [x] `IGeographicSortingService`
    - [x] `INodeGenerationService`
    - [x] `IRouteInterpolationService`
    - [x] `IGridCacheService`
    - [x] `ISchemaDiscoveryService`
- [x] Remove `virtual` keywords used solely for mocking in concrete classes
- [x] Update `ProgressionEngines` to use interface injection

## Phase 4: Data Integrity & Features
- [ ] Implement database storage for GPX generation timestamps
- [ ] Implement logic to compare `.fit` file timestamps with `.gpx` timestamps in `SessionValidator`
- [ ] Resolve local environment networking issues for `IntegrationTests`
