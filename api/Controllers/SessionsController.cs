using Microsoft.AspNetCore.Mvc;
using Bikeapelago.Api.Models;
using Bikeapelago.Api.Repositories;
using Bikeapelago.Api.Services;

namespace Bikeapelago.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SessionsController : ControllerBase
    {
        private readonly IGameSessionRepository _sessionRepository;
        private readonly IUserRepository _userRepository;
        private readonly PocketBaseService _pb;

        public SessionsController(
            IGameSessionRepository sessionRepository,
            IUserRepository userRepository,
            PocketBaseService pb)
        {
            _sessionRepository = sessionRepository;
            _userRepository = userRepository;
            _pb = pb;
        }

        [HttpGet]
        public async Task<IActionResult> GetSessions()
        {
            try {
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                    return Unauthorized(new { message = "No auth token provided" });

                var token = authHeader.Substring("Bearer ".Length).Trim();

                _pb.Token = token;
                var user = await _userRepository.GetCurrentUserAsync(token);
                if (user == null)
                    return Unauthorized(new { message = "Invalid token" });

                var sessions = await _sessionRepository.GetByUserIdAsync(user.Id);
                return Ok(sessions);
            } catch (Exception ex) {
                return StatusCode(500, ex.ToString());
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetSession(string id)
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
        public async Task<IActionResult> GenerateSessionNodes(string id, [FromBody] Bikeapelago.Api.Services.NodeGenerationRequest request, [FromServices] Bikeapelago.Api.Services.NodeGenerationService nodeGenerationService)
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
    }
}
