using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Bikeapelago.Api.Services;

namespace Bikeapelago.Api.Controllers;

/// <summary>
/// API endpoints for elevation data queries.
/// Supports single-point lookups and elevation profiles for routes.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ElevationController : ControllerBase
{
    private readonly ElevationService _elevationService;
    private readonly ILogger<ElevationController> _logger;

    public ElevationController(
        ElevationService elevationService,
        ILogger<ElevationController> logger)
    {
        _elevationService = elevationService;
        _logger = logger;
    }

    /// <summary>
    /// Get elevation at a single coordinate.
    /// Returns elevation in meters or null if outside coverage area.
    /// </summary>
    /// <param name="lat">Latitude (WGS84)</param>
    /// <param name="lon">Longitude (WGS84)</param>
    /// <example>
    /// GET /api/elevation/point?lat=40.712&lon=-74.006
    /// </example>
    [HttpGet("point")]
    public async Task<IActionResult> GetPointElevation(
        [Required] double lat,
        [Required] double lon)
    {
        if (Math.Abs(lat) > 90 || Math.Abs(lon) > 180)
            return BadRequest(new { error = "Invalid coordinates" });

        try
        {
            var elevation = await _elevationService.GetElevationAsync(lat, lon);

            return Ok(new PointElevationResponse
            {
                Latitude = lat,
                Longitude = lon,
                ElevationMeters = elevation
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error querying elevation: {ex.Message}");
            return StatusCode(500, new { error = "Failed to query elevation data" });
        }
    }

    /// <summary>
    /// Get elevation profile for a route (polyline).
    /// Returns array of distance/elevation samples along the route.
    /// </summary>
    /// <param name="request">Route coordinates and sampling options</param>
    /// <example>
    /// POST /api/elevation/profile
    /// {
    ///   "coordinates": [[40.712, -74.006], [40.758, -73.985]],
    ///   "sampleIntervalMeters": 100
    /// }
    /// </example>
    [HttpPost("profile")]
    public async Task<IActionResult> GetElevationProfile([FromBody] ElevationProfileRequest request)
    {
        if (request?.Coordinates == null || request.Coordinates.Count < 2)
            return BadRequest(new { error = "At least 2 coordinates required" });

        if (request.SampleIntervalMeters < 10 || request.SampleIntervalMeters > 10000)
            return BadRequest(new { error = "Sample interval must be between 10 and 10000 meters" });

        try
        {
            var routePoints = request.Coordinates
                .Select(c => (c[0], c[1]))
                .ToList();

            var profile = await _elevationService.GetElevationProfileAsync(
                routePoints,
                request.SampleIntervalMeters);

            // Calculate stats
            var elevations = profile.Where(p => p.elevationMeters.HasValue)
                .Select(p => (double)p.elevationMeters!.Value)
                .ToList();

            return Ok(new ElevationProfileResponse
            {
                Profile = profile.Select(p => (object)new { p.distanceMeters, p.elevationMeters }).ToList(),
                Statistics = new
                {
                    TotalDistanceMeters = profile.LastOrDefault().distanceMeters,
                    MinElevation = elevations.Any() ? (int?)elevations.Min() : null,
                    MaxElevation = elevations.Any() ? (int?)elevations.Max() : null,
                    AvgElevation = elevations.Any() ? (int?)elevations.Average() : null,
                    ElevationGain = CalculateElevationGain(profile),
                    ElevationLoss = CalculateElevationLoss(profile)
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error querying elevation profile: {ex.Message}");
            return StatusCode(500, new { error = "Failed to calculate elevation profile" });
        }
    }

    /// <summary>
    /// Get elevation coverage statistics.
    /// Shows how many nodes have elevation data and min/max/avg values.
    /// </summary>
    /// <example>
    /// GET /api/elevation/stats
    /// </example>
    [HttpGet("stats")]
    public async Task<IActionResult> GetElevationStats()
    {
        try
        {
            var isAvailable = await _elevationService.IsElevationDataAvailableAsync();
            if (!isAvailable)
                return StatusCode(503, new { error = "Elevation data not available" });

            var stats = await _elevationService.GetElevationStatsAsync();
            if (stats == null)
                return StatusCode(500, new { error = "Failed to retrieve statistics" });

            return Ok(new
            {
                available = true,
                totalNodes = stats.TotalNodes,
                nodesWithElevation = stats.NodesWithElevation,
                coveragePercent = stats.CoveragePercent,
                minElevationMeters = stats.MinElevationMeters,
                maxElevationMeters = stats.MaxElevationMeters,
                avgElevationMeters = stats.AvgElevationMeters
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting elevation stats: {ex.Message}");
            return StatusCode(500, new { error = "Failed to retrieve statistics" });
        }
    }

private static int? CalculateElevationGain(List<(double distanceMeters, int? elevationMeters)> profile)
    {
        if (profile.Count < 2)
            return null;

        int totalGain = 0;
        for (int i = 1; i < profile.Count; i++)
        {
            if (profile[i].elevationMeters.HasValue && profile[i - 1].elevationMeters.HasValue)
            {
                var delta = profile[i].elevationMeters!.Value - profile[i - 1].elevationMeters!.Value;
                if (delta > 0)
                    totalGain += delta;
            }
        }
        return totalGain > 0 ? totalGain : null;
    }

    private static int? CalculateElevationLoss(List<(double distanceMeters, int? elevationMeters)> profile)
    {
        if (profile.Count < 2)
            return null;

        int totalLoss = 0;
        for (int i = 1; i < profile.Count; i++)
        {
            if (profile[i].elevationMeters.HasValue && profile[i - 1].elevationMeters.HasValue)
            {
                var delta = profile[i].elevationMeters!.Value - profile[i - 1].elevationMeters!.Value;
                if (delta < 0)
                    totalLoss += Math.Abs(delta);
            }
        }
        return totalLoss > 0 ? totalLoss : null;
    }
}

/// <summary>
/// Request DTO for elevation at a single point.
/// </summary>
public class PointElevationResponse
{
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    [JsonPropertyName("elevation_m")]
    public int? ElevationMeters { get; set; }
}

/// <summary>
/// Request DTO for elevation profile.
/// </summary>
public class ElevationProfileRequest
{
    [JsonPropertyName("coordinates")]
    [Required(ErrorMessage = "Coordinates required")]
    public List<double[]>? Coordinates { get; set; }

    [JsonPropertyName("sample_interval_m")]
    public int SampleIntervalMeters { get; set; } = 100; // Default: sample every 100m
}

/// <summary>
/// Response DTO for elevation profile.
/// </summary>
public class ElevationProfileResponse
{
    [JsonPropertyName("profile")]
    public List<object>? Profile { get; set; }

    [JsonPropertyName("statistics")]
    public object? Statistics { get; set; }
}
