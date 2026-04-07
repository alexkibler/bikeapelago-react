using Microsoft.AspNetCore.Mvc;
using Bikeapelago.Api.Models;
using Bikeapelago.Api.Repositories;
using Bikeapelago.Api.Services;
using System.Text.Json.Serialization;

namespace Bikeapelago.Api.Controllers;

[ApiController]
[Route("api/sessions/{sessionId}/nodes")]
public class NodesController(IMapNodeRepository nodeRepository) : ControllerBase
{
    private readonly IMapNodeRepository _nodeRepository = nodeRepository;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MapNode>>> GetSessionNodes(Guid sessionId)
    {
        var nodes = await _nodeRepository.GetBySessionIdAsync(sessionId);
        return Ok(nodes);
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

        if (request.State != null)
        {
            node.State = request.State;
        }

        var updated = await _nodeRepository.UpdateAsync(node);
        return Ok(updated);
    }
}
