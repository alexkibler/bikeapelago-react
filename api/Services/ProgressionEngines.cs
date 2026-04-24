using System;
using System.Linq;
using System.Threading.Tasks;
using Bikeapelago.Api.Models;
using Bikeapelago.Api.Repositories;
using Microsoft.Extensions.Logging;

namespace Bikeapelago.Api.Services;

public interface IProgressionEngine
{
    Task UnlockNextAsync(Guid sessionId);
    Task CheckNodesAsync(Guid sessionId, List<MapNode> targetNodes);
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

    public async Task UnlockNextAsync(Guid sessionId)
    {
        _logger.LogInformation("Single player unlock trigger for session {SessionId}", sessionId);

        var session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session == null) return;

        if (session.ProgressionMode == "quadrant")
        {
            await HandleQuadrantUnlockAsync(session);
        }
        else if (session.ProgressionMode == "radius")
        {
            await HandleRadiusUnlockAsync(session);
        }
        else 
        {
            // Default/Free mode: unlock next deterministic node
            await HandleFreeUnlockAsync(session);
        }
    }

    private async Task HandleQuadrantUnlockAsync(GameSession session)
    {
        var missingPasses = new List<long>();
        if (!session.NorthPassReceived) missingPasses.Add(802002);
        if (!session.SouthPassReceived) missingPasses.Add(802003);
        if (!session.EastPassReceived) missingPasses.Add(802004);
        if (!session.WestPassReceived) missingPasses.Add(802005);

        if (missingPasses.Count == 0)
        {
            await GrantRandomUsefulItemAsync(session);
            return;
        }

        var passToGrant = missingPasses[new Random().Next(missingPasses.Count)];
        _logger.LogInformation("Granting Quadrant Pass {ItemId} to Session {SessionId}", passToGrant, session.Id);
        
        session.ReceivedItemIds.Add(passToGrant);
        await _sessionRepository.UpdateAsync(session);
        await _archipelagoService.UpdateUnlockedNodesAsync(session.Id, session.ReceivedItemIds.ToArray());
        await _archipelagoService.BroadcastMessageAsync(session.Id, $"Received {_archipelagoService.GetItemName(session.Id, passToGrant)}!", "item");
    }

    private async Task HandleRadiusUnlockAsync(GameSession session)
    {
        if (session.RadiusStep < 3)
        {
            _logger.LogInformation("Granting Radius Increase to Session {SessionId}", session.Id);
            long itemId = 802006;
            session.ReceivedItemIds.Add(itemId);
            await _sessionRepository.UpdateAsync(session);
            await _archipelagoService.UpdateUnlockedNodesAsync(session.Id, session.ReceivedItemIds.ToArray());
            await _archipelagoService.BroadcastMessageAsync(session.Id, $"Received {_archipelagoService.GetItemName(session.Id, itemId)}!", "item");
        }
        else
        {
            await GrantRandomUsefulItemAsync(session);
        }
    }

    private async Task HandleFreeUnlockAsync(GameSession session)
    {
        var nodes = await _nodeRepository.GetBySessionIdAsync(session.Id);
        var nextNode = nodes
            .Where(n => n.State == "Hidden")
            .OrderBy(n => n.ApArrivalLocationId)
            .FirstOrDefault();

        if (nextNode != null)
        {
            _logger.LogInformation("Unlocking next deterministic node: {NodeId} ({Name})", nextNode.Id, nextNode.Name);
            nextNode.State = "Available";
            await _nodeRepository.UpdateAsync(nextNode);
            await _archipelagoService.BroadcastMessageAsync(session.Id, $"Received {nextNode.Name} Reveal!", "item");
        }
    }

    private async Task GrantRandomUsefulItemAsync(GameSession session)
    {
        long[] useful = [802010, 802011, 802012]; // Detour, Drone, Signal Amp
        var item = useful[new Random().Next(useful.Length)];
        session.ReceivedItemIds.Add(item);
        await _sessionRepository.UpdateAsync(session);
        await _archipelagoService.UpdateUnlockedNodesAsync(session.Id, session.ReceivedItemIds.ToArray());
        await _archipelagoService.BroadcastMessageAsync(session.Id, $"Received {_archipelagoService.GetItemName(session.Id, item)}!", "item");
    }

    public async Task CheckNodesAsync(Guid sessionId, List<MapNode> targetNodes)
    {
        var nodesToUpdate = targetNodes.Where(n => n.State != "Checked").ToList();
        if (nodesToUpdate.Count == 0) return;

        foreach (var node in nodesToUpdate)
        {
            node.State = "Checked";
            await _archipelagoService.BroadcastMessageAsync(sessionId, $"Cleared {node.Name}!", "system");
        }

        await _nodeRepository.UpdateRangeAsync(nodesToUpdate);
        _logger.LogInformation("Marked {Count} node(s) as Checked for singleplayer Session {SessionId}", nodesToUpdate.Count, sessionId);

        for (int i = 0; i < nodesToUpdate.Count; i++)
            await UnlockNextAsync(sessionId);
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

    public async Task CheckNodesAsync(Guid sessionId, List<MapNode> targetNodes)
    {
        var locationsToCheck = new List<long>();
        foreach (var node in targetNodes)
        {
            if (node.IsArrivalChecked) locationsToCheck.Add(node.ApArrivalLocationId);
            if (node.IsPrecisionChecked) locationsToCheck.Add(node.ApPrecisionLocationId);
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
