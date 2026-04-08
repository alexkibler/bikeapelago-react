using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Bikeapelago.Api.Models;
using Microsoft.Extensions.Logging;
using OsmSharp;
using OsmSharp.Streams;

namespace Bikeapelago.Api.Services;

public class PbfOsmDiscoveryService(ILogger<PbfOsmDiscoveryService> logger, string pbfPath) : IOsmDiscoveryService
{
    private readonly ILogger<PbfOsmDiscoveryService> _logger = logger;
    private readonly string _pbfPath = pbfPath;

    public async Task<List<DiscoveryPoint>> GetRandomNodesAsync(double lat, double lon, double radiusMeters, int count, string mode = "bike")
    {
        _logger.LogInformation("Streaming random nodes from PBF file at {Path} near {Lat},{Lon} radius {Radius}m, mode: {Mode}", _pbfPath, lat, lon, radiusMeters, mode);

        if (!File.Exists(_pbfPath))
        {
            throw new FileNotFoundException("PBF file not found.", _pbfPath);
        }

        // Two-pass approach to find nodes on highways within radius
        // Pass 1: Find all node IDs on highway ways
        var highwayNodeIds = new HashSet<long>();
        using (var fileStream = File.OpenRead(_pbfPath))
        {
            var source = new PBFOsmStreamSource(fileStream);
            foreach (var element in source)
            {
                if (element is Way way && IsBikeFriendly(way))
                {
                    if (way.Nodes != null)
                    {
                        foreach (var nodeId in way.Nodes)
                        {
                            highwayNodeIds.Add(nodeId);
                        }
                    }
                }
            }
        }

        _logger.LogInformation("Found {Count} candidate node IDs on bike-friendly ways", highwayNodeIds.Count);

        // Pass 2: Extract those nodes that are within the radius
        var candidateNodes = new List<DiscoveryPoint>();
        using (var fileStream = File.OpenRead(_pbfPath))
        {
            var source = new PBFOsmStreamSource(fileStream);
            foreach (var element in source)
            {
                if (element is Node node && highwayNodeIds.Contains(node.Id!.Value))
                {
                    if (node.Latitude.HasValue && node.Longitude.HasValue)
                    {
                        double dist = CalculateDistance(lat, lon, node.Latitude.Value, node.Longitude.Value);
                        if (dist <= radiusMeters)
                        {
                            candidateNodes.Add(new DiscoveryPoint(node.Longitude.Value, node.Latitude.Value));
                        }
                    }
                }
            }
        }

        _logger.LogInformation("Found {Count} candidate nodes within search radius", candidateNodes.Count);

        // Shuffle and take 'count'
        var random = new Random();
        for (int i = candidateNodes.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (candidateNodes[i], candidateNodes[j]) = (candidateNodes[j], candidateNodes[i]);
        }

        return candidateNodes.Take(count).ToList();
    }

    private static bool IsBikeFriendly(Way way)
    {
        if (way.Tags == null) return false;
        if (!way.Tags.TryGetValue("highway", out var highway)) return false;

        var allowed = new[] { "cycleway", "residential", "tertiary", "path", "track", "living_street" };
        if (!allowed.Contains(highway)) return false;

        if (way.Tags.TryGetValue("access", out var access) && access == "private") return false;

        return true;
    }

    private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        // Haversine formula
        double r = 6371000; // meters
        double phi1 = lat1 * Math.PI / 180;
        double phi2 = lat2 * Math.PI / 180;
        double dphi = (lat2 - lat1) * Math.PI / 180;
        double dlambda = (lon2 - lon1) * Math.PI / 180;

        double a = Math.Sin(dphi / 2) * Math.Sin(dphi / 2) +
                   Math.Cos(phi1) * Math.Cos(phi2) *
                   Math.Sin(dlambda / 2) * Math.Sin(dlambda / 2);
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return r * c;
    }

    public Task<List<ValidateResult>> ValidateNodesAsync(ValidateRequest request)
    {
        throw new NotImplementedException("PBF validation not implemented yet.");
    }
}
