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
    ILogger<SinglePlayerProgressionEngine> logger) : IProgressionEngine
{
    private readonly IMapNodeRepository _nodeRepository = nodeRepository;
    private readonly ILogger<SinglePlayerProgressionEngine> _logger = logger;

    public async Task UnlockNextAsync(Guid sessionId)
    {
        _logger.LogInformation("Single player unlock trigger for session {SessionId}", sessionId);

        var nodes = await _nodeRepository.GetBySessionIdAsync(sessionId);
        var nextNode = nodes
            .Where(n => n.State == "Hidden")
            .OrderBy(n => n.ApLocationId)
            .FirstOrDefault();

        if (nextNode != null)
        {
            _logger.LogInformation("Unlocking next deterministic node: {NodeId} ({Name})", nextNode.Id, nextNode.Name);
            nextNode.State = "Available";
            await _nodeRepository.UpdateAsync(nextNode);
        }
        else
        {
            _logger.LogInformation("No hidden nodes left to unlock for session {SessionId}", sessionId);
        }
    }

    public async Task CheckNodesAsync(Guid sessionId, List<MapNode> targetNodes)
    {
        var nodesToUpdate = targetNodes.Where(n => n.State != "Checked").ToList();
        if (nodesToUpdate.Count == 0) return;

        foreach (var node in nodesToUpdate)
            node.State = "Checked";

        await _nodeRepository.UpdateRangeAsync(nodesToUpdate);
        _logger.LogInformation("Marked {Count} node(s) as Checked for singleplayer Session {SessionId}", nodesToUpdate.Count, sessionId);

        for (int i = 0; i < nodesToUpdate.Count; i++)
            await UnlockNextAsync(sessionId);
    }
}

public class ArchipelagoProgressionEngine(ArchipelagoService archipelagoService, IMapNodeRepository nodeRepository, ILogger<ArchipelagoProgressionEngine> logger) : IProgressionEngine
{
    private readonly ArchipelagoService _archipelagoService = archipelagoService;
    private readonly IMapNodeRepository _nodeRepository = nodeRepository;
    private readonly ILogger<ArchipelagoProgressionEngine> _logger = logger;

    public async Task UnlockNextAsync(Guid sessionId)
    {
        _logger.LogInformation("Archipelago progression trigger for session {SessionId}", sessionId);

        var nodes = await _nodeRepository.GetBySessionIdAsync(sessionId);
        var checkedNodes = nodes.Where(n => n.State == "Checked").ToList();

        if (checkedNodes.Count > 0)
        {
            var locationIds = checkedNodes.Select(n => n.ApLocationId).ToArray();
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
        var locationIds = targetNodes.Select(n => n.ApLocationId).ToArray();
        _logger.LogInformation("Sending {Count} location(s) to Archipelago for Session {SessionId}", locationIds.Length, sessionId);
        await _archipelagoService.CheckLocationsAsync(sessionId, locationIds);
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
