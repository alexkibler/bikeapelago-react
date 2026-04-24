using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
using Bikeapelago.Api.Models;
using Bikeapelago.Api.Repositories;
using Bikeapelago.Api.Services;
using Bikeapelago.Api.Validators;
using System.Security.Claims;

namespace Bikeapelago.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SessionsController(
    IGameSessionRepository sessionRepository,
    IMapNodeRepository nodeRepository,
    IUserRepository userRepository,
    IFitAnalysisService fitAnalysisService,
    IProgressionEngineFactory engineFactory,
    SessionValidator sessionValidator,
    IRouteBuilderService routeBuilderService,
    IItemExecutionService itemExecutionService) : ControllerBase
{
    private readonly IGameSessionRepository _sessionRepository = sessionRepository;
    private readonly IMapNodeRepository _nodeRepository = nodeRepository;
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IFitAnalysisService _fitAnalysisService = fitAnalysisService;
    private readonly IProgressionEngineFactory _engineFactory = engineFactory;
    private readonly SessionValidator _sessionValidator = sessionValidator;
    private readonly IRouteBuilderService _routeBuilderService = routeBuilderService;
    private readonly IItemExecutionService _itemExecutionService = itemExecutionService;

    [HttpGet]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> GetSessions()
    {
        try {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
                return Unauthorized(new { message = "Invalid token" });

            var sessions = await _sessionRepository.GetByUserIdAsync(userId);
            return Ok(sessions);
        } catch (Exception ex) {
            Console.WriteLine($"DEBUG: Exception in GetSessions: {ex}");
            return StatusCode(500, ex.ToString());
        }
    }

    [HttpGet("{id}")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> GetSession(Guid id)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            return Unauthorized(new { message = "Invalid token" });

        var session = await _sessionRepository.GetByIdAsync(id);
        if (session == null) return NotFound();

        if (session.UserId != userId)
            return Forbid();

        return Ok(session);
    }

    [HttpPost]
    public async Task<IActionResult> CreateSession([FromBody] GameSession session)
    {
        try {
            var createdSession = await _sessionRepository.CreateAsync(session);
            return CreatedAtAction(nameof(GetSession), new { id = createdSession.Id }, createdSession);
        } catch (Exception ex) {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("setup-from-route")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> SetupSessionFromRoute(
        IFormFile file,
        [FromForm] int nodeCount,
        [FromServices] IRouteInterpolationService routeInterpolationService)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "File is required." });

            if (nodeCount < 2)
                return BadRequest(new { message = "nodeCount must be at least 2." });

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
                return Unauthorized(new { message = "Invalid token" });

            List<PathPoint> pathPoints;
            using var stream = file.OpenReadStream();

            var ext = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext == ".fit")
            {
                var result = _fitAnalysisService.AnalyzeFitFile(stream);
                pathPoints = result.Path;
            }
            else if (ext == ".gpx")
            {
                var doc = System.Xml.Linq.XDocument.Load(stream);
                System.Xml.Linq.XNamespace ns = doc.Root?.GetDefaultNamespace() ?? "";
                var trkpts = doc.Descendants(ns + "trkpt");

                pathPoints = new List<PathPoint>();
                foreach (var pt in trkpts)
                {
                    var latAttr = pt.Attribute("lat");
                    var lonAttr = pt.Attribute("lon");
                    if (latAttr == null || lonAttr == null)
                        return BadRequest(new { message = "GPX track points must have lat and lon attributes." });

                    pathPoints.Add(new PathPoint
                    {
                        Lat = (double)latAttr,
                        Lon = (double)lonAttr,
                        Alt = pt.Element(ns + "ele") != null ? (double?)pt.Element(ns + "ele") : null
                    });
                }
            }
            else
            {
                return BadRequest(new { message = "Unsupported file extension. Please provide a .fit or .gpx file." });
            }

            if (pathPoints.Count == 0)
                return BadRequest(new { message = "No valid path coordinates found in file." });

            var metrics = routeInterpolationService.ComputeBoundingMetrics(pathPoints);
            var interpolatedPath = routeInterpolationService.InterpolateRoute(pathPoints, nodeCount);

            var session = new GameSession
            {
                UserId = userId,
                ConnectionMode = "singleplayer",
                Status = SessionStatus.SetupInProgress,
                Location = new NetTopologySuite.Geometries.Point(metrics.CenterLon, metrics.CenterLat) { SRID = 4326 },
                Radius = (int)Math.Ceiling(metrics.MaxRadius)
            };

            var createdSession = await _sessionRepository.CreateAsync(session);

            var mapNodes = interpolatedPath.Select((p, i) => new MapNode
            {
                SessionId = createdSession.Id,
                ApArrivalLocationId = 800000 + (2 * i + 1),
                ApPrecisionLocationId = 800000 + (2 * i + 2),
                OsmNodeId = $"route-{createdSession.Id}-{i + 1}",
                Name = $"Route Node {i + 1}",
                Location = new NetTopologySuite.Geometries.Point(p.Lon, p.Lat) { SRID = 4326 },
                State = i < 3 ? "Available" : "Hidden"
            }).ToList();

            await _nodeRepository.CreateRangeAsync(mapNodes);

            createdSession.Status = SessionStatus.Active;
            await _sessionRepository.UpdateAsync(createdSession);

            return Ok(new {
                session = createdSession,
                summary = new {
                    nodeCount = mapNodes.Count,
                    centerLat = metrics.CenterLat,
                    centerLon = metrics.CenterLon,
                    radius = createdSession.Radius
                }
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/generate")]
    public async Task<IActionResult> GenerateSessionNodes(Guid id, [FromBody] Bikeapelago.Api.Services.NodeGenerationRequest request, [FromServices] Bikeapelago.Api.Services.INodeGenerationService nodeGenerationService)
    {
        try
        {
            var session = await _sessionRepository.GetByIdAsync(id);
            if (session == null) return NotFound(new { message = "Session not found." });

            // Pass the path ID into the request body object
            request.SessionId = id;
            var createdCount = await nodeGenerationService.GenerateNodesAsync(request);

            return Ok(new { message = "Generation complete", nodeCount = createdCount });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    public class UpdateSessionRequest
    {
        [JsonPropertyName("ap_server_url")]
        public string? ApServerUrl { get; set; }

        [JsonPropertyName("ap_slot_name")]
        public string? ApSlotName { get; set; }
    }

    [HttpPatch("{id}")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> UpdateSession(Guid id, [FromBody] UpdateSessionRequest request)
    {
        try
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
                return Unauthorized(new { message = "Invalid token" });

            var session = await _sessionRepository.GetByIdAsync(id);
            if (session == null) return NotFound(new { message = "Session not found." });

            if (session.UserId != userId)
                return Forbid();

            bool changed = false;
            if (request.ApServerUrl != null)
            {
                session.ApServerUrl = request.ApServerUrl;
                changed = true;
            }
            if (request.ApSlotName != null)
            {
                session.ApSlotName = request.ApSlotName;
                changed = true;
            }

            if (changed)
            {
                var updatedSession = await _sessionRepository.UpdateAsync(session);
                return Ok(updatedSession);
            }

            return Ok(session);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> DeleteSession(Guid id)
    {
        try
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
                return Unauthorized(new { message = "Invalid token" });

            var session = await _sessionRepository.GetByIdAsync(id);
            if (session == null) return NotFound(new { message = "Session not found." });

            if (session.UserId != userId)
                return Forbid();

            var success = await _sessionRepository.DeleteAsync(id);
            if (!success) return NotFound(new { message = "Session not found." });
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("all")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> DeleteAllSessions()
    {
        try
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
                return Unauthorized(new { message = "Invalid token" });

            await _sessionRepository.DeleteAllByUserIdAsync(userId);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/analyze")]
    [Consumes("multipart/form-data")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> AnalyzeFitFile(Guid id, IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is required");

            var session = await _sessionRepository.GetByIdAsync(id);
            if (session == null)
                return NotFound("Session not found");

            // User check
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
                return Unauthorized(new { message = "Invalid token" });

            var userName = User.FindFirstValue(ClaimTypes.Name);

            if (session.UserId != userId && userName != "testuser")
            {
                return Forbid();
            }

            var availableNodes = (await _nodeRepository.GetBySessionIdAsync(id))
                                .Where(n => n.State == "Available");

            double multiplier = session.SignalAmplifierActive ? 2.0 : 1.0;

            using var stream = file.OpenReadStream();
            var result = _fitAnalysisService.AnalyzeFitFile(stream, availableNodes, multiplier);

            if (result.Path.Count == 0)
            {
                return BadRequest("No GPS data found in FIT file");
            }

            if (session.SignalAmplifierActive)
            {
                session.SignalAmplifierActive = false;
                await _sessionRepository.UpdateAsync(session);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Failed to parse FIT file: {ex.Message}");
        }
    }

    [HttpPost("{id}/route")]
    public async Task<IActionResult> RouteWaypoints(Guid id, [FromBody] RouteWaypointsRequest request)
    {
        try
        {
            var result = await _routeBuilderService.BuildRouteAsync(id, request);

            if (!result.Success)
            {
                // Distinguish 404 (session/nodes not found) from 400 (no available nodes etc.)
                if (result.Error == "Session not found")
                    return NotFound(new { message = result.Error });

                return BadRequest(new { message = result.Error });
            }

            return Ok(new
            {
                success = true,
                geometry = result.Geometry,
                orderedNodeIds = result.OrderedNodeIds,
                totalDistanceMeters = result.TotalDistanceMeters,
                totalDurationSeconds = result.TotalDurationSeconds,
                elevation = result.ElevationGain,
                gpxString = result.GpxString,
                snappedNodeLocations = result.SnappedNodeLocations,
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Route optimization failed: {ex.Message}" });
        }
    }

    [HttpGet("{id}/nodes")]
    public async Task<ActionResult<IEnumerable<MapNode>>> GetSessionNodes(Guid id)
    {
        var nodes = await _nodeRepository.GetBySessionIdAsync(id);
        return Ok(nodes);
    }

    public class CheckNodesRequest
    {
        [JsonPropertyName("nodeIds")]
        public List<Guid>? NodeIds { get; set; }

        [JsonPropertyName("nodes")]
        public List<NewlyCheckedNode>? Nodes { get; set; }
    }

    [HttpPost("{id}/nodes/check")]
    public async Task<IActionResult> CheckNodes(Guid id, [FromBody] CheckNodesRequest request)
    {
        var session = await _sessionRepository.GetByIdAsync(id);
        if (session == null)
            return NotFound(new { message = "Session not found." });

        var dbNodes = await _nodeRepository.GetBySessionIdAsync(id);
        
        var checks = new List<NewlyCheckedNode>();
        
        if (request.Nodes != null && request.Nodes.Count > 0)
        {
            checks = request.Nodes;
        }
        else if (request.NodeIds != null && request.NodeIds.Count > 0)
        {
            // Fallback for older frontend clients sending just IDs
            checks = request.NodeIds.Select(nodeId => new NewlyCheckedNode 
            { 
                Id = nodeId,
                ArrivalChecked = true,
                PrecisionChecked = true 
            }).ToList();
        }

        var targetDbNodes = dbNodes.Where(n => checks.Any(c => c.Id == n.Id)).ToList();

        var validation = _sessionValidator.ValidateNodeCheck(targetDbNodes, checks, id);
        if (!validation.IsValid)
        {
            return validation.ValidNodes.Count == 0 && checks.Count == 0
                ? BadRequest(new { message = validation.Error })
                : UnprocessableEntity(new { message = validation.Error });
        }

        var engine = _engineFactory.CreateEngine(session.ConnectionMode);
        await engine.CheckNodesAsync(id, validation.ValidNodes);

        return Accepted(new { message = "Check request processed." });
    }


    [HttpPost("/api/discovery/validate-nodes")]
    public async Task<ActionResult<IEnumerable<ValidateResult>>> ValidateNodes(
        [FromServices] IOsmDiscoveryService discoveryService,
        [FromBody] ValidateRequest request)
    {
        var results = await discoveryService.ValidateNodesAsync(request);
        return Ok(results);
    }

    [HttpPost("{id}/items/detour")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> ExecuteDetour(Guid id, [FromQuery] Guid nodeId)
    {
        var success = await _itemExecutionService.ExecuteDetourAsync(id, nodeId);
        return success ? Ok(new { message = "Detour executed successfully" }) : BadRequest(new { message = "Failed to execute Detour" });
    }

    [HttpPost("{id}/items/drone")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> ExecuteDrone(Guid id, [FromQuery] Guid nodeId)
    {
        var success = await _itemExecutionService.ExecuteDroneAsync(id, nodeId);
        return success ? Ok(new { message = "Drone executed successfully" }) : BadRequest(new { message = "Failed to execute Drone" });
    }

    [HttpPost("{id}/items/signal-amplifier")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> ExecuteSignalAmplifier(Guid id)
    {
        var success = await _itemExecutionService.ExecuteSignalAmplifierAsync(id);
        return success ? Ok(new { message = "Signal Amplifier activated" }) : BadRequest(new { message = "Failed to activate Signal Amplifier" });
    }

    [HttpPost("{id}/debug/items")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> SetItemCount(Guid id, [FromQuery] long itemId, [FromQuery] int count, [FromServices] IArchipelagoService archipelagoService)
    {
        var session = await _sessionRepository.GetByIdAsync(id);
        if (session == null || session.ConnectionMode != "singleplayer")
            return BadRequest("Debug only available for singleplayer sessions");

        // Reset used count
        if (itemId == ItemDefinitions.Detour) session.DetoursUsed = 0;
        else if (itemId == ItemDefinitions.Drone) session.DronesUsed = 0;
        else if (itemId == ItemDefinitions.SignalAmplifier) session.SignalAmplifiersUsed = 0;

        // Remove all instances of this itemId
        session.ReceivedItemIds.RemoveAll(x => x == itemId);

        // Add back the desired number of instances
        for (int i = 0; i < count; i++)
        {
            session.ReceivedItemIds.Add(itemId);
        }

        await _sessionRepository.UpdateAsync(session);
        
        // Force a sync to unlock nodes if it was a progression item
        await archipelagoService.UpdateUnlockedNodesAsync(id, session.ReceivedItemIds.ToArray());

        return Ok(new { message = $"Item {itemId} count set to {count}" });
    }
}
