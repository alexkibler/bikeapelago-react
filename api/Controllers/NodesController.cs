using Microsoft.AspNetCore.Mvc;
using Bikeapelago.Api.Models;
using Bikeapelago.Api.Repositories;
using Bikeapelago.Api.Services;
using System.Text.Json.Serialization;
using System.Linq;

namespace Bikeapelago.Api.Controllers;

[ApiController]
[Route("api/sessions/{sessionId}/nodes")]
public class NodesController(IMapNodeRepository nodeRepository, ILogger<NodesController> logger) : ControllerBase
{
    private readonly IMapNodeRepository _nodeRepository = nodeRepository;
    private readonly ILogger<NodesController> _logger = logger;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MapNode>>> GetSessionNodes(Guid sessionId)
    {
        var nodes = await _nodeRepository.GetBySessionIdAsync(sessionId);
        return Ok(nodes);
    }

    public class CheckNodesRequest
    {
        [JsonPropertyName("nodeIds")]
        public List<Guid> NodeIds { get; set; } = [];
    }

    [HttpPost("check")]
    public async Task<IActionResult> CheckNodes(Guid sessionId, [FromServices] ArchipelagoService archipelagoService, [FromBody] CheckNodesRequest request)
    {
        _logger.LogInformation("Received check request for {Count} nodes in Session {SessionId}", request.NodeIds.Count, sessionId);
        var nodes = await _nodeRepository.GetBySessionIdAsync(sessionId);
        var targetNodes = nodes.Where(n => request.NodeIds.Contains(n.Id)).ToList();
        
        if (targetNodes.Count == 0) 
        {
            _logger.LogWarning("No valid nodes found matching the IDs provided for Session {SessionId}", sessionId);
            return BadRequest("No valid nodes found to check.");
        }

        var locationIds = targetNodes.Select(n => n.ApLocationId).ToArray();
        _logger.LogInformation("Resolved {Count} Archipelago locations for checking: {LocationIds}", locationIds.Length, string.Join(", ", locationIds));
        
        await archipelagoService.CheckLocationsAsync(sessionId, locationIds);

        return Accepted(new { message = "Check request sent to Archipelago." });
    }

    [HttpPost("/api/discovery/validate-nodes")]
    public async Task<ActionResult<IEnumerable<ValidateResult>>> ValidateNodes(
        [FromServices] IOsmDiscoveryService discoveryService,
        [FromBody] ValidateRequest request)
    {
        var results = await discoveryService.ValidateNodesAsync(request);
        return Ok(results);
    }
}

[ApiController]
[Route("api/nodes")]
public class NodeUpdateController(IMapNodeRepository nodeRepository) : ControllerBase
{
    private readonly IMapNodeRepository _nodeRepository = nodeRepository;

    public class PatchNodeRequest
    {
        [JsonPropertyName("state")]
        public string? State { get; set; }
    }

    [HttpPatch("{id}")]
    public async Task<ActionResult<MapNode>> PatchNode(Guid id, [FromBody] PatchNodeRequest request)
    {
        var node = await _nodeRepository.GetByIdAsync(id);
        if (node == null) return NotFound(new { message = "Node not found." });

        // We only allow patching to states OTHER than Checked manually, 
        // OR we just don't allow patching state here if we want to be strict.
        // For now, let's just make it so it doesn't trigger Archipelago here anymore.
        
        if (request.State != null)
        {
            node.State = request.State;
        }

        var updated = await _nodeRepository.UpdateAsync(node);
        return Ok(updated);
    }
}
