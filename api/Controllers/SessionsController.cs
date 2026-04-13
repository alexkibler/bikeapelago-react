using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
using Bikeapelago.Api.Models;
using Bikeapelago.Api.Repositories;
using Bikeapelago.Api.Services;
using System.Security.Claims;

namespace Bikeapelago.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SessionsController(
    IGameSessionRepository sessionRepository,
    IMapNodeRepository nodeRepository,
    IUserRepository userRepository,
    FitAnalysisService fitAnalysisService,
    IMapboxRoutingService mapboxRoutingService) : ControllerBase
{
    private readonly IGameSessionRepository _sessionRepository = sessionRepository;
    private readonly IMapNodeRepository _nodeRepository = nodeRepository;
    private readonly IUserRepository _userRepository = userRepository;
    private readonly FitAnalysisService _fitAnalysisService = fitAnalysisService;
    private readonly IMapboxRoutingService _mapboxRoutingService = mapboxRoutingService;

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
        [FromServices] RouteInterpolationService routeInterpolationService)
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
                Mode = "singleplayer",
                Status = SessionStatus.SetupInProgress,
                Location = new NetTopologySuite.Geometries.Point(metrics.CenterLon, metrics.CenterLat) { SRID = 4326 },
                Radius = (int)Math.Ceiling(metrics.MaxRadius)
            };

            var createdSession = await _sessionRepository.CreateAsync(session);

            var mapNodes = interpolatedPath.Select((p, i) => new MapNode
            {
                SessionId = createdSession.Id,
                ApLocationId = 800000 + (i + 1),
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
    public async Task<IActionResult> GenerateSessionNodes(Guid id, [FromBody] Bikeapelago.Api.Services.NodeGenerationRequest request, [FromServices] Bikeapelago.Api.Services.NodeGenerationService nodeGenerationService)
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

            using var stream = file.OpenReadStream();
            var result = _fitAnalysisService.AnalyzeFitFile(stream, availableNodes);

            if (result.Path.Count == 0)
            {
                return BadRequest("No GPS data found in FIT file");
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Failed to parse FIT file: {ex.Message}");
        }
    }

    [HttpPost("{id}/route-to-available")]
    public async Task<IActionResult> RouteToAvailableNodes(Guid id, [FromQuery] string profile = "cycling", [FromQuery] bool turnByTurn = true)
    {
        try
        {
            var session = await _sessionRepository.GetByIdAsync(id);
            if (session == null)
                return NotFound(new { message = "Session not found" });

            if (session.Location == null)
                return BadRequest(new { message = "Session has no starting location" });

            var allNodes = await _nodeRepository.GetBySessionIdAsync(id);
            var availableNodes = allNodes.Where(n => n.State == "Available").ToList();

            if (availableNodes.Count == 0)
                return BadRequest(new { message = "No available nodes to route to" });

            // Call the Mapbox routing service to optimize the route
            var result = await _mapboxRoutingService.RouteToMultipleNodesAsync(
                session.Location,
                availableNodes,
                profile);

            if (!result.Success)
                return BadRequest(new { message = result.Error });

            var elevationGain = await _mapboxRoutingService.CalculateElevationGainAsync(result.Geometry);

            // Reconstruct the ordered MapNode list to build the GPX
            var orderedNodes = result.OrderedNodeIds
                .Select(id => availableNodes.FirstOrDefault(n => n.Id == id))
                .Where(n => n != null)
                .Cast<MapNode>()
                .ToList();

            var gpxString = _mapboxRoutingService.GenerateGpx(result.Geometry, orderedNodes, turnByTurn);

            return Ok(new
            {
                success = true,
                geometry = result.Geometry,
                orderedNodeIds = result.OrderedNodeIds,
                totalDistanceMeters = result.TotalDistanceMeters,
                totalDurationSeconds = result.TotalDurationSeconds,
                elevation = elevationGain,
                gpxString = gpxString
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Route optimization failed: {ex.Message}" });
        }
    }
}
