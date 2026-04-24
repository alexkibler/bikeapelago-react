using System;
using System.Linq;
using System.Threading.Tasks;
using Bikeapelago.Api.Models;
using Bikeapelago.Api.Repositories;
using Microsoft.Extensions.Logging;

namespace Bikeapelago.Api.Services;

public interface IProgressionEngine
{
    Task CheckNodesAsync(Guid sessionId, List<NewlyCheckedNode> checks);
}

public class SinglePlayerProgressionEngine(
    IMapNodeRepository nodeRepository,
    IGameSessionRepository sessionRepository,
    IArchipelagoService archipelagoService,
    ILogger<SinglePlayerProgressionEngine> logger) : IProgressionEngine
{
    private readonly IMapNodeRepository _nodeRepository = nodeRepository;
    private readonly IGameSessionRepository _sessionRepository = sessionRepository;
    private readonly IArchipelagoService _archipelagoService = archipelagoService;
    private readonly ILogger<SinglePlayerProgressionEngine> _logger = logger;

    public async Task CheckNodesAsync(Guid sessionId, List<NewlyCheckedNode> checks)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session == null || checks.Count == 0) return;

        var nodeIds = checks.Select(c => c.Id).ToList();
        var targetNodes = (await _nodeRepository.GetBySessionIdAsync(sessionId))
            .Where(n => nodeIds.Contains(n.Id)).ToList();

        var nodesToUpdate = new List<MapNode>();
        bool sessionChanged = false;

        foreach (var check in checks)
        {
            var node = targetNodes.FirstOrDefault(n => n.Id == check.Id);
            if (node == null) continue;

            bool nodeChanged = false;

            // Sequential/Simultaneous Transition Check for Arrival
            if (!node.IsArrivalChecked && check.ArrivalChecked)
            {
                node.IsArrivalChecked = true;
                nodeChanged = true;

                if (node.ArrivalRewardItemId.HasValue)
                {
                    session.ReceivedItemIds.Add(node.ArrivalRewardItemId.Value);
                    sessionChanged = true;
                    _logger.LogInformation("Node {NodeId}: Granting Arrival Reward {ItemId}", node.Id, node.ArrivalRewardItemId.Value);
                    await _archipelagoService.BroadcastMessageAsync(sessionId, $"Received {node.ArrivalRewardItemName}!", "item");

                    if (node.ArrivalRewardItemId.Value == ItemDefinitions.Macguffin)
                    {
                        session.MacguffinsCollected++;
                        sessionChanged = true;
                    }
                }
            }

            // Sequential/Simultaneous Transition Check for Precision
            if (!node.IsPrecisionChecked && check.PrecisionChecked)
            {
                node.IsPrecisionChecked = true;
                nodeChanged = true;

                if (node.PrecisionRewardItemId.HasValue)
                {
                    session.ReceivedItemIds.Add(node.PrecisionRewardItemId.Value);
                    sessionChanged = true;
                    _logger.LogInformation("Node {NodeId}: Granting Precision Reward {ItemId}", node.Id, node.PrecisionRewardItemId.Value);
                    await _archipelagoService.BroadcastMessageAsync(sessionId, $"Received {node.PrecisionRewardItemName}!", "item");

                    if (node.PrecisionRewardItemId.Value == ItemDefinitions.Macguffin)
                    {
                        session.MacguffinsCollected++;
                        sessionChanged = true;
                    }
                }
            }

            if (node.IsArrivalChecked && node.IsPrecisionChecked && node.State != "Checked")
            {
                node.State = "Checked";
                nodeChanged = true;
                await _archipelagoService.BroadcastMessageAsync(sessionId, $"Cleared {node.Name}!", "system");
            }

            if (nodeChanged)
            {
                nodesToUpdate.Add(node);
            }
        }

        if (nodesToUpdate.Count > 0)
        {
            await _nodeRepository.UpdateRangeAsync(nodesToUpdate);
            _logger.LogInformation("Updated {Count} node(s) for singleplayer Session {SessionId}", nodesToUpdate.Count, sessionId);
        }

        if (sessionChanged)
        {
            if (session.MacguffinsRequired > 0 &&
                session.MacguffinsCollected >= session.MacguffinsRequired &&
                session.Status != SessionStatus.Completed)
            {
                session.Status = SessionStatus.Completed;
                _logger.LogInformation("Session {SessionId} completed! Collected {Collected}/{Required} Macguffins.", sessionId, session.MacguffinsCollected, session.MacguffinsRequired);
                await _archipelagoService.BroadcastMessageAsync(sessionId, "You collected all required Macguffins! Congratulations!", "system");
            }

            await _sessionRepository.UpdateAsync(session);
            // Ensure UI and nodes are synced after reward grants
            await _archipelagoService.UpdateUnlockedNodesAsync(sessionId, session.ReceivedItemIds.ToArray());
        }
    }
}

public class ArchipelagoProgressionEngine(IArchipelagoService archipelagoService, IMapNodeRepository nodeRepository, ILogger<ArchipelagoProgressionEngine> logger) : IProgressionEngine
{
    private readonly IArchipelagoService _archipelagoService = archipelagoService;
    private readonly IMapNodeRepository _nodeRepository = nodeRepository;
    private readonly ILogger<ArchipelagoProgressionEngine> _logger = logger;

    public async Task UnlockNextAsync(Guid sessionId)
    {
        _logger.LogInformation("Archipelago progression trigger for session {SessionId}", sessionId);

        var nodes = await _nodeRepository.GetBySessionIdAsync(sessionId);
        var locationsToCheck = new List<long>();
        
        foreach (var node in nodes)
        {
            if (node.IsArrivalChecked) locationsToCheck.Add(node.ApArrivalLocationId);
            if (node.IsPrecisionChecked) locationsToCheck.Add(node.ApPrecisionLocationId);
        }

        if (locationsToCheck.Count > 0)
        {
            var locationIds = locationsToCheck.Distinct().ToArray();
            _logger.LogInformation("Sending {Count} checked locations to Archipelago for Session {SessionId}", locationIds.Length, sessionId);
            await _archipelagoService.CheckLocationsAsync(sessionId, locationIds);
        }
        else
        {
            _logger.LogInformation("No checked nodes found to sync for session {SessionId}", sessionId);
        }
    }

    public async Task CheckNodesAsync(Guid sessionId, List<NewlyCheckedNode> checks)
    {
        var locationsToCheck = new List<long>();
        foreach (var check in checks)
        {
            if (check.ArrivalChecked) locationsToCheck.Add(check.ApArrivalLocationId);
            if (check.PrecisionChecked) locationsToCheck.Add(check.ApPrecisionLocationId);
        }

        if (locationsToCheck.Count > 0)
        {
            var locationIds = locationsToCheck.Distinct().ToArray();
            _logger.LogInformation("Sending {Count} location(s) to Archipelago for Session {SessionId}", locationIds.Length, sessionId);
            await _archipelagoService.CheckLocationsAsync(sessionId, locationIds);
        }
    }
}

public interface IProgressionEngineFactory
{
    IProgressionEngine CreateEngine(string gameMode);
}

public class ProgressionEngineFactory(
    SinglePlayerProgressionEngine singlePlayerEngine,
    ArchipelagoProgressionEngine archipelagoEngine) : IProgressionEngineFactory
{
    public IProgressionEngine CreateEngine(string gameMode) =>
        gameMode == "singleplayer" ? singlePlayerEngine : archipelagoEngine;
}
