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

    public Task<List<DiscoveryPoint>> GetRandomNodesAsync(double lat, double lon, double radiusMeters, int count, string mode = "bike", double densityBias = 0.5)
    {
        return _impl.GetRandomNodesAsync(lat, lon, radiusMeters, count, mode, densityBias);
    }

    public Task<List<DiscoveryPoint>> GetRandomNodesInWedgeAsync(double lat, double lon, double radiusMeters, double startDeg, double endDeg, int count, string mode = "bike", double densityBias = 0.5, double minRadiusMeters = 0.0)
    {
        return _impl.GetRandomNodesInWedgeAsync(lat, lon, radiusMeters, startDeg, endDeg, count, mode, densityBias, minRadiusMeters);
    }

    public Task<List<ValidateResult>> ValidateNodesAsync(ValidateRequest request)
    {
        return _impl.ValidateNodesAsync(request);
    }

    private class MockOsmDiscoveryService : IOsmDiscoveryService
    {
        public Task<List<DiscoveryPoint>> GetRandomNodesAsync(double lat, double lon, double radiusMeters, int count, string mode = "bike", double densityBias = 0.5)
        {
            return GetRandomNodesInWedgeAsync(lat, lon, radiusMeters, 0, 360, count, mode, densityBias);
        }

        public Task<List<DiscoveryPoint>> GetRandomNodesInWedgeAsync(double lat, double lon, double radiusMeters, double startDeg, double endDeg, int count, string mode = "bike", double densityBias = 0.5, double minRadiusMeters = 0.0)
        {
            var mockPoints = new List<DiscoveryPoint>();
            var random = Random.Shared;

            double radiusDegrees = radiusMeters / 111000.0;
            double startRad = (startDeg % 360) * Math.PI / 180.0;
            double endRad = (endDeg % 360) * Math.PI / 180.0;
            if (endRad <= startRad) endRad += 2.0 * Math.PI;
            double diffRad = endRad - startRad;

            for (int i = 0; i < count; i++)
            {
                double angle = startRad + (random.NextDouble() * diffRad);
                double mathAngle = (Math.PI / 2.0) - angle;
                
                double distance = Math.Sqrt(random.NextDouble()) * radiusDegrees;

                double randomLon = lon + (distance * Math.Cos(mathAngle));
                double randomLat = lat + (distance * Math.Sin(mathAngle));

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
