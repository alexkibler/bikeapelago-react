using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bikeapelago.Api.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bikeapelago.Api.Services;

public class OsmDiscoveryService : IOsmDiscoveryService
{
    private readonly IOsmDiscoveryService _impl;
    private readonly ILogger<OsmDiscoveryService> _logger;

    public OsmDiscoveryService(IServiceProvider serviceProvider, IConfiguration configuration, ILogger<OsmDiscoveryService> logger)
    {
        _logger = logger;

        if (Environment.GetEnvironmentVariable("USE_MOCK_OVERPASS") == "true" || configuration["USE_MOCK_OVERPASS"] == "true")
        {
            _logger.LogInformation("Using Mock OSM Discovery Service");
            _impl = new MockOsmDiscoveryService();
        }
        else if (!string.IsNullOrEmpty(configuration["OsmDiscovery:PbfPath"]))
        {
            _logger.LogInformation("Using PBF OSM Discovery Service");
            _impl = serviceProvider.GetRequiredService<PbfOsmDiscoveryService>();
        }
        else if (!string.IsNullOrEmpty(configuration.GetConnectionString("OsmDiscovery")))
        {
            _logger.LogInformation("Using PostGIS OSM Discovery Service");
            _impl = serviceProvider.GetRequiredService<PostGisOsmDiscoveryService>();
        }
        else
        {
            _logger.LogInformation("Using Overpass OSM Discovery Service (fallback)");
            _impl = serviceProvider.GetRequiredService<OverpassOsmDiscoveryService>();
        }
    }

    public Task<List<DiscoveryPoint>> GetRandomNodesAsync(double lat, double lon, double radiusMeters, int count, string mode = "bike", double densityBias = 0.5, string gameMode = "singleplayer")
    {
        return _impl.GetRandomNodesAsync(lat, lon, radiusMeters, count, mode, densityBias, gameMode);
    }

    public Task<List<ValidateResult>> ValidateNodesAsync(ValidateRequest request)
    {
        return _impl.ValidateNodesAsync(request);
    }

    private class MockOsmDiscoveryService : IOsmDiscoveryService
    {
        public Task<List<DiscoveryPoint>> GetRandomNodesAsync(double lat, double lon, double radiusMeters, int count, string mode = "bike", double densityBias = 0.5, string gameMode = "singleplayer")
        {
            var mockPoints = new List<DiscoveryPoint>();
            var random = Random.Shared;

            // ~0.00001 degrees per meter (111 meters per degree)
            double radiusDegrees = radiusMeters / 111000.0;

            for (int i = 0; i < count; i++)
            {
                // Random point within circle using polar coordinates
                double angle = random.NextDouble() * 2 * Math.PI;
                double distance = Math.Sqrt(random.NextDouble()) * radiusDegrees;

                double randomLon = lon + (distance * Math.Cos(angle));
                double randomLat = lat + (distance * Math.Sin(angle));

                mockPoints.Add(new DiscoveryPoint(randomLon, randomLat));
            }
            return Task.FromResult(mockPoints);
        }

        public Task<List<ValidateResult>> ValidateNodesAsync(ValidateRequest request)
        {
            var results = new List<ValidateResult>();
            foreach (var p in request.Points)
            {
                results.Add(new ValidateResult(Original: p, IsValid: true, RoadName: "Mock road"));
            }
            return Task.FromResult(results);
        }
    }
}
