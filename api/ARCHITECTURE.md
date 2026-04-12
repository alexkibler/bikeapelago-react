## Progression Engines

Different game modes use different progression strategies via `IProgressionEngine`:

- **SinglePlayerProgressionEngine**: Deterministic unlock - each checked node unlocks the next sequential node by ApLocationId
- **ArchipelagoProgressionEngine**: Integrates with Archipelago server - sends checked locations to sync game state

The `IProgressionEngineFactory` selects the appropriate engine based on `session.Mode`.

Usage in NodesController.PatchNode:
```csharp
var engine = _engineFactory.CreateEngine(session.Mode);
await engine.UnlockNextAsync(session.Id);
```
