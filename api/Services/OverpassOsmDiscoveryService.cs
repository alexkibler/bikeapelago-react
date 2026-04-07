using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Bikeapelago.Api.Models;
using Microsoft.Extensions.Logging;

namespace Bikeapelago.Api.Services;

public class OverpassOsmDiscoveryService(HttpClient httpClient, ILogger<OverpassOsmDiscoveryService> logger) : IOsmDiscoveryService
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<OverpassOsmDiscoveryService> _logger = logger;
    private const string OverpassUrl = "https://overpass-api.de/api/interpreter";

    public async Task<List<DiscoveryPoint>> GetRandomNodesAsync(double lat, double lon, double radiusMeters, int count)
    {
        _logger.LogInformation("Fetching random nodes from Overpass API at {Lat},{Lon} radius {Radius}m", lat, lon, radiusMeters);

        // Overpass QL to find nodes near the center point. 
        // We'll filter for some bike-friendly tags.
        // We request nodes within the radius and then we'll randomly sample locally.
        // Query ways with bike-friendly highway tags, then get their member nodes.
        // Nodes themselves don't carry highway tags in OSM — only the parent way does.
        var query = $"[out:json][timeout:25];way(around:{radiusMeters},{lat},{lon})[\"highway\"~\"cycleway|residential|tertiary|path|track|living_street\"];node(w);out body;";
        
        var response = await _httpClient.GetAsync($"{OverpassUrl}?data={Uri.EscapeDataString(query)}");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var elements = doc.RootElement.GetProperty("elements");
        
        var allPoints = new List<DiscoveryPoint>();
        foreach (var element in elements.EnumerateArray())
        {
            allPoints.Add(new DiscoveryPoint(element.GetProperty("lon").GetDouble(), element.GetProperty("lat").GetDouble()));
        }

        // Shuffle and take 'count'
        var random = new Random();
        for (int i = allPoints.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (allPoints[i], allPoints[j]) = (allPoints[j], allPoints[i]);
        }

        return allPoints.GetRange(0, Math.Min(count, allPoints.Count));
    }

    public Task<List<ValidateResult>> ValidateNodesAsync(ValidateRequest request)
    {
        // Overpass validation is basically just checking if they still exist or have tags, 
        // but for now we'll just return them all as valid or use a simple distance-based check.
        // Real validation would use GraphHopper as before.
        throw new NotImplementedException("Validation not implemented for Overpass fallback yet.");
    }
}
