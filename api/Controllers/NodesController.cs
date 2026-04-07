using Microsoft.AspNetCore.Mvc;
using Bikeapelago.Api.Models;
using Bikeapelago.Api.Repositories;
using Bikeapelago.Api.Services;

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
