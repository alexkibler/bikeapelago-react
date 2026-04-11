using Microsoft.AspNetCore.Mvc;

namespace Bikeapelago.Api.Controllers;

[ApiController]
[Route("api/geocode")]
public class GeocodeController(IConfiguration config, IHttpClientFactory httpClientFactory, ILogger<GeocodeController> logger) : ControllerBase
{
    private readonly IConfiguration _config = config;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly ILogger<GeocodeController> _logger = logger;

    /// <summary>
    /// Proxy geocoding requests to Mapbox so the API key stays server-side.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Geocode([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { message = "Query parameter 'q' is required." });

        var token = _config["Mapbox:ApiKey"] ?? _config["MAPBOX_API_KEY"];
        if (string.IsNullOrWhiteSpace(token))
            return StatusCode(500, new { message = "Mapbox API key not configured." });

        var encoded = Uri.EscapeDataString(q.Trim());
        var url = $"https://api.mapbox.com/geocoding/v5/mapbox.places/{encoded}.json?access_token={token}&limit=5&types=place,locality,neighborhood,address";

        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Mapbox geocoding returned {Status} for query: {Query}", response.StatusCode, q);
                return StatusCode((int)response.StatusCode, new { message = "Geocoding service error." });
            }

            var json = await response.Content.ReadAsStringAsync();
            return Content(json, "application/json");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Geocoding request failed for query: {Query}", q);
            return StatusCode(500, new { message = "Geocoding request failed." });
        }
    }
}
