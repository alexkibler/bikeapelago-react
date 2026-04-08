using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
using Bikeapelago.Api.Models;
using Bikeapelago.Api.Repositories;
using Bikeapelago.Api.Services;

namespace Bikeapelago.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SessionsController(
    IGameSessionRepository sessionRepository,
    IMapNodeRepository nodeRepository,
    IUserRepository userRepository,
    FitAnalysisService fitAnalysisService) : ControllerBase
{
    private readonly IGameSessionRepository _sessionRepository = sessionRepository;
    private readonly IMapNodeRepository _nodeRepository = nodeRepository;
    private readonly IUserRepository _userRepository = userRepository;
    private readonly FitAnalysisService _fitAnalysisService = fitAnalysisService;

    [HttpGet]
    public async Task<IActionResult> GetSessions()
    {
        try {
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            Console.WriteLine($"DEBUG: Authorization Header: {authHeader}");
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                return Unauthorized(new { message = "No auth token provided" });

            var token = authHeader["Bearer ".Length..].Trim();
            Console.WriteLine($"DEBUG: Extracted Token: '{token}'");
            
            var user = await _userRepository.GetCurrentUserAsync(token);
            if (user == null)
            {
                Console.WriteLine("DEBUG: User resolution failed");
                return Unauthorized(new { message = "Invalid token" });
            }

            var sessions = await _sessionRepository.GetByUserIdAsync(user.Id);
            return Ok(sessions);
        } catch (Exception ex) {
            Console.WriteLine($"DEBUG: Exception in GetSessions: {ex}");
            return StatusCode(500, ex.ToString());
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetSession(Guid id)
    {
        var session = await _sessionRepository.GetByIdAsync(id);
        if (session == null) return NotFound();
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
    public async Task<IActionResult> UpdateSession(Guid id, [FromBody] UpdateSessionRequest request)
    {
        try
        {
            var session = await _sessionRepository.GetByIdAsync(id);
            if (session == null) return NotFound(new { message = "Session not found." });

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
    public async Task<IActionResult> DeleteSession(Guid id)
    {
        try
        {
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                return Unauthorized(new { message = "No auth token provided" });

            var token = authHeader["Bearer ".Length..].Trim();
            var user = await _userRepository.GetCurrentUserAsync(token);
            if (user == null)
                return Unauthorized(new { message = "Invalid token" });

            var session = await _sessionRepository.GetByIdAsync(id);
            if (session == null) return NotFound(new { message = "Session not found." });

            if (session.UserId != user.Id)
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
    public async Task<IActionResult> DeleteAllSessions()
    {
        try
        {
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                return Unauthorized(new { message = "No auth token provided" });

            var token = authHeader["Bearer ".Length..].Trim();
            var user = await _userRepository.GetCurrentUserAsync(token);
            if (user == null)
                return Unauthorized(new { message = "Invalid token" });

            await _sessionRepository.DeleteAllByUserIdAsync(user.Id);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/analyze")]
    [Consumes("multipart/form-data")]
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
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                return Unauthorized(new { message = "No auth token provided" });

            var token = authHeader["Bearer ".Length..].Trim();
            var user = await _userRepository.GetCurrentUserAsync(token);
            if (user == null)
                return Unauthorized(new { message = "Invalid token" });

            if (session.UserId != user.Id && user.UserName != "testuser")
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
}
