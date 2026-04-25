using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
using Bikeapelago.Api.Models;
using Bikeapelago.Api.Repositories;
using Bikeapelago.Api.Services;
using Bikeapelago.Api.Validators;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;

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
    private static readonly HashSet<string> AllowedTransportModes = new(StringComparer.OrdinalIgnoreCase)
    {
        "bike", "walk", "foot"
    };

    private static readonly HashSet<string> AllowedGameModes = new(StringComparer.OrdinalIgnoreCase)
    {
        "archipelago", "singleplayer", "quadrant", "radius", "free"
    };

    private static readonly HashSet<string> AllowedRoutingProfiles = new(StringComparer.OrdinalIgnoreCase)
    {
        "bike", "cycling", "walk", "walking", "foot", "car", "driving"
    };

    private readonly IGameSessionRepository _sessionRepository = sessionRepository;
    private readonly IMapNodeRepository _nodeRepository = nodeRepository;
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IFitAnalysisService _fitAnalysisService = fitAnalysisService;
    private readonly IProgressionEngineFactory _engineFactory = engineFactory;
    private readonly SessionValidator _sessionValidator = sessionValidator;
    private readonly IRouteBuilderService _routeBuilderService = routeBuilderService;
    private readonly IItemExecutionService _itemExecutionService = itemExecutionService;

    private bool TryGetAuthenticatedUserId(out Guid userId)
    {
        userId = default;
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return !string.IsNullOrEmpty(userIdString) && Guid.TryParse(userIdString, out userId);
    }

    private async Task<(GameSession? Session, IActionResult? Error)> GetAuthorizedSessionResultAsync(Guid id)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return (null, Unauthorized(new { message = "Invalid token" }));

        var session = await _sessionRepository.GetByIdAsync(id);
        if (session == null)
            return (null, NotFound(new { message = "Session not found." }));

        if (session.UserId != userId)
            return (null, Forbid());

        return (session, null);
    }

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
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> CreateSession([FromBody] CreateSessionRequest request)
    {
        try
        {
            if (!TryGetAuthenticatedUserId(out var userId))
                return Unauthorized(new { message = "Invalid token" });

            var validationError = ValidateCreateSessionRequest(request);
            if (validationError != null)
                return BadRequest(new { message = validationError });

            var session = new GameSession
            {
                UserId = userId,
                Name = request.Name,
                Status = SessionStatus.SetupInProgress,
                ConnectionMode = request.ConnectionMode?.ToLowerInvariant() switch
                {
                    "archipelago" => "archipelago",
                    _ => "singleplayer"
                },
                Radius = request.Radius
            };

            if (request.CenterLat.HasValue && request.CenterLon.HasValue)
            {
                session.Location = new NetTopologySuite.Geometries.Point(request.CenterLon.Value, request.CenterLat.Value) { SRID = 4326 };
            }

            var createdSession = await _sessionRepository.CreateAsync(session);
            return CreatedAtAction(nameof(GetSession), new { id = createdSession.Id }, createdSession);
        }
        catch (Exception ex)
        {
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
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> GenerateSessionNodes(Guid id, [FromBody] Bikeapelago.Api.Services.NodeGenerationRequest request, [FromServices] Bikeapelago.Api.Services.INodeGenerationService nodeGenerationService)
    {
        try
        {
            var (_, error) = await GetAuthorizedSessionResultAsync(id);
            if (error != null)
                return error;

            var validationError = ValidateNodeGenerationRequest(request);
            if (validationError != null)
                return BadRequest(new { message = validationError });

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

            if (session.UserId != userId)
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
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> RouteWaypoints(Guid id, [FromBody] RouteWaypointsRequest request)
    {
        try
        {
            var (_, error) = await GetAuthorizedSessionResultAsync(id);
            if (error != null)
                return error;

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
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> GetSessionNodes(Guid id)
    {
        var (_, error) = await GetAuthorizedSessionResultAsync(id);
        if (error != null)
            return error;

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
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> CheckNodes(Guid id, [FromBody] CheckNodesRequest request)
    {
        var (session, error) = await GetAuthorizedSessionResultAsync(id);
        if (error != null || session == null)
            return error ?? NotFound(new { message = "Session not found." });

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

    [HttpPost("{id}/debug/force-complete")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> DebugForceComplete(Guid id)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return Unauthorized(new { message = "Invalid token" });

        var session = await _sessionRepository.GetByIdAsync(id);
        if (session == null)
            return NotFound(new { message = "Session not found." });

        if (session.UserId != userId)
            return Forbid();

        var allNodes = await _nodeRepository.GetBySessionIdAsync(id);
        var nodesToUpdate = new List<MapNode>();

        foreach (var node in allNodes)
        {
            bool changed = false;

            if (!node.IsArrivalChecked)
            {
                node.IsArrivalChecked = true;
                if (node.ArrivalRewardItemId.HasValue)
                {
                    session.ReceivedItemIds.Add(node.ArrivalRewardItemId.Value);
                    if (node.ArrivalRewardItemId.Value == ItemDefinitions.Macguffin)
                        session.MacguffinsCollected++;
                }
                changed = true;
            }

            if (!node.IsPrecisionChecked)
            {
                node.IsPrecisionChecked = true;
                if (node.PrecisionRewardItemId.HasValue)
                {
                    session.ReceivedItemIds.Add(node.PrecisionRewardItemId.Value);
                    if (node.PrecisionRewardItemId.Value == ItemDefinitions.Macguffin)
                        session.MacguffinsCollected++;
                }
                changed = true;
            }

            if (changed)
            {
                node.State = "Checked";
                nodesToUpdate.Add(node);
            }
        }

        if (nodesToUpdate.Count > 0)
            await _nodeRepository.UpdateRangeAsync(nodesToUpdate);

        session.Status = SessionStatus.Completed;
        await _sessionRepository.UpdateAsync(session);

        return Ok(new { message = "Session force-completed." });
    }

    [HttpPost("/api/discovery/validate-nodes")]
    public async Task<ActionResult<IEnumerable<ValidateResult>>> ValidateNodes(
        [FromServices] IOsmDiscoveryService discoveryService,
        [FromBody] ValidateRequest request)
    {
        var validationError = ValidateDiscoveryRequest(request);
        if (validationError != null)
            return BadRequest(new { message = validationError });

        var results = await discoveryService.ValidateNodesAsync(request);
        return Ok(results);
    }

    [HttpPost("{id}/items/detour")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> ExecuteDetour(Guid id, [FromQuery] Guid nodeId)
    {
        if (nodeId == Guid.Empty)
            return BadRequest(new { message = "nodeId is required." });

        var (_, error) = await GetAuthorizedSessionResultAsync(id);
        if (error != null)
            return error;

        var success = await _itemExecutionService.ExecuteDetourAsync(id, nodeId);
        return success ? Ok(new { message = "Detour executed successfully" }) : BadRequest(new { message = "Failed to execute Detour" });
    }

    [HttpPost("{id}/items/drone")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> ExecuteDrone(Guid id, [FromQuery] Guid nodeId)
    {
        if (nodeId == Guid.Empty)
            return BadRequest(new { message = "nodeId is required." });

        var (_, error) = await GetAuthorizedSessionResultAsync(id);
        if (error != null)
            return error;

        var success = await _itemExecutionService.ExecuteDroneAsync(id, nodeId);
        return success ? Ok(new { message = "Drone executed successfully" }) : BadRequest(new { message = "Failed to execute Drone" });
    }

    [HttpPost("{id}/items/signal-amplifier")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> ExecuteSignalAmplifier(Guid id)
    {
        var (_, error) = await GetAuthorizedSessionResultAsync(id);
        if (error != null)
            return error;

        var success = await _itemExecutionService.ExecuteSignalAmplifierAsync(id);
        return success ? Ok(new { message = "Signal Amplifier activated" }) : BadRequest(new { message = "Failed to activate Signal Amplifier" });
    }

    [HttpPost("{id}/debug/items")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> SetItemCount(Guid id, [FromQuery] long itemId, [FromQuery] int count, [FromServices] IArchipelagoService archipelagoService)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return Unauthorized(new { message = "Invalid token" });

        if (count < 0 || count > 99)
            return BadRequest(new { message = "count must be between 0 and 99" });

        if (itemId == ItemDefinitions.Macguffin || !ItemDefinitions.ItemNames.ContainsKey(itemId))
            return BadRequest(new { message = "Unsupported debug item" });

        var session = await _sessionRepository.GetByIdAsync(id);
        if (session == null || session.ConnectionMode != "singleplayer")
            return BadRequest("Debug only available for singleplayer sessions");

        if (session.UserId != userId)
            return Forbid();

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

    public class CreateSessionRequest
    {
        [JsonPropertyName("name")]
        [MaxLength(200)]
        public string? Name { get; set; }

        [JsonPropertyName("center_lat")]
        public double? CenterLat { get; set; }

        [JsonPropertyName("center_lon")]
        public double? CenterLon { get; set; }

        [JsonPropertyName("radius")]
        public int? Radius { get; set; }

        [JsonPropertyName("connection_mode")]
        public string? ConnectionMode { get; set; } = "singleplayer";
    }

    private static string? ValidateCreateSessionRequest(CreateSessionRequest request)
    {
        if (request.CenterLat.HasValue && (request.CenterLat.Value < -90 || request.CenterLat.Value > 90))
            return "center_lat must be between -90 and 90.";

        if (request.CenterLon.HasValue && (request.CenterLon.Value < -180 || request.CenterLon.Value > 180))
            return "center_lon must be between -180 and 180.";

        if ((request.CenterLat.HasValue && !request.CenterLon.HasValue) || (!request.CenterLat.HasValue && request.CenterLon.HasValue))
            return "center_lat and center_lon must either both be set or both be omitted.";

        if (request.Radius.HasValue && (request.Radius.Value <= 0 || request.Radius.Value > 1_000_000))
            return "radius must be between 1 and 1000000 meters.";

        if (!string.IsNullOrWhiteSpace(request.ConnectionMode) &&
            !request.ConnectionMode.Equals("singleplayer", StringComparison.OrdinalIgnoreCase) &&
            !request.ConnectionMode.Equals("archipelago", StringComparison.OrdinalIgnoreCase))
            return "connection_mode must be either 'singleplayer' or 'archipelago'.";

        return null;
    }

    private static string? ValidateNodeGenerationRequest(NodeGenerationRequest request)
    {
        if (request.CenterLat < -90 || request.CenterLat > 90)
            return "centerLat must be between -90 and 90.";

        if (request.CenterLon < -180 || request.CenterLon > 180)
            return "centerLon must be between -180 and 180.";

        if (request.Radius <= 0 || request.Radius > 1_000_000)
            return "radius must be between 1 and 1000000 meters.";

        if (request.NodeCount < 1 || request.NodeCount > 5000)
            return "nodeCount must be between 1 and 5000.";

        if (request.DensityBias <= 0 || request.DensityBias > 1.0)
            return "densityBias must be greater than 0 and less than or equal to 1.0.";

        if (!AllowedTransportModes.Contains(request.TransportMode))
            return "transportMode must be one of: bike, walk, foot.";

        if (!AllowedGameModes.Contains(request.GameMode))
            return "gameMode must be one of: archipelago, singleplayer, quadrant, radius, free.";

        return null;
    }

    private static string? ValidateDiscoveryRequest(ValidateRequest request)
    {
        if (request.Points == null || request.Points.Length == 0)
            return "points must contain at least one coordinate.";

        if (request.Points.Length > 1000)
            return "points must contain at most 1000 coordinates.";

        if (!AllowedRoutingProfiles.Contains(request.Profile))
            return "profile must be one of: bike, cycling, walk, walking, foot, car, driving.";

        for (int i = 0; i < request.Points.Length; i++)
        {
            var p = request.Points[i];
            if (p.Lat < -90 || p.Lat > 90)
                return $"points[{i}].lat must be between -90 and 90.";
            if (p.Lon < -180 || p.Lon > 180)
                return $"points[{i}].lon must be between -180 and 180.";
        }

        return null;
    }
}
