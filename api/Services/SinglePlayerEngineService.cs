using System;
using System.Linq;
using System.Threading.Tasks;
using Bikeapelago.Api.Models;
using Bikeapelago.Api.Repositories;
using Microsoft.Extensions.Logging;

namespace Bikeapelago.Api.Services;

public class SinglePlayerEngineService(
    IMapNodeRepository nodeRepository,
    ILogger<SinglePlayerEngineService> logger)
{
    private readonly IMapNodeRepository _nodeRepository = nodeRepository;
    private readonly ILogger<SinglePlayerEngineService> _logger = logger;

    public async Task UnlockNextNodeAsync(Guid sessionId)
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
}
