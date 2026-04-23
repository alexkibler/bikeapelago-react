using Bikeapelago.Api.Authorization;
using Bikeapelago.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Bikeapelago.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/[controller]")]
[AdminAuthorize]
public class SchemaController(ISchemaDiscoveryService schemaService) : ControllerBase
{
    private readonly ISchemaDiscoveryService _schemaService = schemaService;

    [HttpGet]
    public IActionResult GetSchema()
    {
        var schema = _schemaService.GetSchema();
        return Ok(schema);
    }
}
