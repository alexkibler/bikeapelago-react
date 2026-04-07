using Microsoft.AspNetCore.Mvc;
using Bikeapelago.Api.Models;
using Bikeapelago.Api.Repositories;

namespace Bikeapelago.Api.Controllers
{
    [ApiController]
    [Route("api/sessions/{sessionId}/nodes")]
    public class NodesController : ControllerBase
    {
        private readonly IMapNodeRepository _nodeRepository;

        public NodesController(IMapNodeRepository nodeRepository)
        {
            _nodeRepository = nodeRepository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MapNode>>> GetSessionNodes(string sessionId)
        {
            var nodes = await _nodeRepository.GetBySessionIdAsync(sessionId);
            return Ok(nodes);
        }
    }
}
