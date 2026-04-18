using Microsoft.AspNetCore.Mvc;
using Bikeapelago.Api.Models;
using Bikeapelago.Api.Repositories;
using Bikeapelago.Api.Services;
using System.Text.Json.Serialization;
using System.Linq;
using System.Security.Claims;

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

    [HttpGet("/api/discovery/test-random-nodes")]
    public async Task<IActionResult> TestRandomNodes(
        [FromServices] IOsmDiscoveryService discoveryService,
        [FromQuery] double lat,
        [FromQuery] double lon,
        [FromQuery] double radiusMeters = 10000,
        [FromQuery] int count = 100,
        [FromQuery] string mode = "bike")
    {
        if (mode != "bike" && mode != "walk")
            return BadRequest(new { error = "mode must be 'bike' or 'walk'" });

        var startTime = DateTime.UtcNow;
        var nodes = await discoveryService.GetRandomNodesAsync(lat, lon, radiusMeters, count, mode);
        var elapsed = DateTime.UtcNow - startTime;

        return Ok(new
        {
            mode = mode,
            requestedCount = count,
            returnedCount = nodes.Count,
            elapsedMs = elapsed.TotalMilliseconds,
        });
    }

    [HttpPost("/api/discovery/route")]
    public async Task<IActionResult> RouteWaypoints(
        [FromServices] IMapboxRoutingService mapboxRoutingService,
        [FromBody] RouteRequest request)
    {
        if (request.Waypoints.Count < 2)
            return BadRequest(new { message = "At least two waypoints are required." });

        _logger.LogInformation("Routing {Count} waypoints for profile {Profile}", request.Waypoints.Count, request.Profile);

        try
        {
            var result = await mapboxRoutingService.OptimizeRouteAsync(request.Waypoints, request.Profile);
            
            if (result == null || result.Code != "Ok" || result.Trips.Count == 0)
                return BadRequest(new { message = result?.Message ?? "Route optimization failed." });

            var trip = result.Trips[0];
            var geometry = trip.GetCoordinates();
            var elevationGain = await mapboxRoutingService.CalculateElevationGainAsync(geometry);
            
            return Ok(new
            {
                success = true,
                geometry = geometry,
                distanceMeters = trip.Distance,
                durationSeconds = trip.Duration,
                elevation = elevationGain
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error optimizing manual route");
            return StatusCode(500, new { message = ex.Message });
        }
    }
}

[ApiController]
[Route("api/nodes")]
public class NodeUpdateController(
    IMapNodeRepository nodeRepository,
    IGameSessionRepository sessionRepository,
    IProgressionEngineFactory engineFactory) : ControllerBase
{
    private readonly IMapNodeRepository _nodeRepository = nodeRepository;
    private readonly IGameSessionRepository _sessionRepository = sessionRepository;
    private readonly IProgressionEngineFactory _engineFactory = engineFactory;

    public class PatchNodeRequest
    {
        [JsonPropertyName("state")]
        public string? State { get; set; }
    }

    [HttpPatch("{id}")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<ActionResult<MapNode>> PatchNode(Guid id, [FromBody] PatchNodeRequest request)
    {
        var userIdString = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            return Unauthorized(new { message = "Invalid token" });

        var node = await _nodeRepository.GetByIdAsync(id);
        if (node == null) return NotFound(new { message = "Node not found." });

        var session = await _sessionRepository.GetByIdAsync(node.SessionId);
        if (session == null) return NotFound(new { message = "Session not found." });
        if (session.UserId != userId) return Forbid();

        if (request.State != null)
        {
            var oldState = node.State;
            node.State = request.State;

            // Trigger engine progression
            if (oldState != "Checked" && request.State == "Checked")
            {
                var engine = _engineFactory.CreateEngine(session.Mode);
                await engine.UnlockNextAsync(session.Id);
            }
        }

        var updated = await _nodeRepository.UpdateAsync(node);
        return Ok(updated);
    }
}
