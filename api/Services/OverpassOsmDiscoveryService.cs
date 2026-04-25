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

    public async Task<List<DiscoveryPoint>> GetRandomNodesAsync(double lat, double lon, double radiusMeters, int count, string mode = "bike", double densityBias = 0.5)
    {
        return await GetRandomNodesInWedgeAsync(lat, lon, radiusMeters, 0, 360, count, mode, densityBias);
    }

    public async Task<List<DiscoveryPoint>> GetRandomNodesInWedgeAsync(double lat, double lon, double radiusMeters, double startDeg, double endDeg, int count, string mode = "bike", double densityBias = 0.5, double minRadiusMeters = 0.0)
    {
        _logger.LogInformation("Fetching random nodes from Overpass API at {Lat},{Lon} radius {Radius}m, wedge {Start}-{End}, mode: {Mode}", lat, lon, radiusMeters, startDeg, endDeg, mode);

        // Overpass QL to find nodes near the center point. 
        var query = $"[out:json][timeout:25];way(around:{radiusMeters},{lat},{lon})[\"highway\"~\"cycleway|residential|tertiary|path|track|living_street\"];node(w);out body;";
        
        var response = await _httpClient.GetAsync($"{OverpassUrl}?data={Uri.EscapeDataString(query)}");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var elements = doc.RootElement.GetProperty("elements");
        
        var allPoints = new List<DiscoveryPoint>();
        foreach (var element in elements.EnumerateArray())
        {
            double pLat = element.GetProperty("lat").GetDouble();
            double pLon = element.GetProperty("lon").GetDouble();

            // Filter by wedge locally
            double az = CalculateAzimuth(lat, lon, pLat, pLon);
            if (IsInWedge(az, startDeg, endDeg))
            {
                allPoints.Add(new DiscoveryPoint(pLon, pLat));
            }
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

    private double CalculateAzimuth(double lat1, double lon1, double lat2, double lon2)
    {
        double lat1Rad = lat1 * Math.PI / 180.0;
        double lat2Rad = lat2 * Math.PI / 180.0;
        double dLonRad = (lon2 - lon1) * Math.PI / 180.0;

        double y = Math.Sin(dLonRad) * Math.Cos(lat2Rad);
        double x = Math.Cos(lat1Rad) * Math.Sin(lat2Rad) - Math.Sin(lat1Rad) * Math.Cos(lat2Rad) * Math.Cos(dLonRad);
        double brng = Math.Atan2(y, x);
        return (brng * 180.0 / Math.PI + 360.0) % 360.0;
    }

    private bool IsInWedge(double az, double start, double end)
    {
        if (Math.Abs(start - 0) < 0.01 && Math.Abs(end - 360) < 0.01) return true;
        if (start < end) return az >= start && az <= end;
        return az >= start || az <= end;
    }

    public Task<List<ValidateResult>> ValidateNodesAsync(ValidateRequest request)
    {
        // Overpass validation is basically just checking if they still exist or have tags,
        // but for now we'll just return them all as valid or use a simple distance-based check.
        throw new NotImplementedException("Validation not implemented for Overpass fallback yet.");
    }
}
