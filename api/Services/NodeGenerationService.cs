using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Bikeapelago.Api.Models;
using Bikeapelago.Api.Repositories;
using Microsoft.Extensions.Logging;

namespace Bikeapelago.Api.Services;

public class NodeGenerationRequest
{
    public Guid SessionId { get; set; }
    public double CenterLat { get; set; }
    public double CenterLon { get; set; }
    public double Radius { get; set; }
    public int NodeCount { get; set; } = 50;
    public string Mode { get; set; } = "bike"; // "bike" or "walk"
    public string GameMode { get; set; } = "archipelago"; // "archipelago" or "singleplayer"

    // Controls how sub-target probes are distributed within the parent radius:
    //   0.5  = Uniform area distribution (geographically fair, sparse center) — default
    //   0.75 = "Goldilocks" zone (denser center for routing, still spreads to edges)
    //   1.0  = Linear distance distribution (heavy center clustering)
    public double DensityBias { get; set; } = 0.5;
}

public class NodeGenerationService(
    IOsmDiscoveryService osmDiscoveryService,
    IMapNodeRepository nodeRepository,
    IGameSessionRepository sessionRepository,
    ILogger<NodeGenerationService> logger) : INodeGenerationService
{
    private readonly IOsmDiscoveryService _osmDiscoveryService = osmDiscoveryService;
    private readonly IMapNodeRepository _nodeRepository = nodeRepository;
    private readonly IGameSessionRepository _sessionRepository = sessionRepository;
    private readonly ILogger<NodeGenerationService> _logger = logger;

    public async Task<int> GenerateNodesAsync(NodeGenerationRequest request)
    {
        var total = Stopwatch.StartNew();
        var sw = Stopwatch.StartNew();

        // 1. Verify session exists and is not already active
        var session = await _sessionRepository.GetByIdAsync(request.SessionId)
            ?? throw new Exception("Session not found");

        if (session.Status != SessionStatus.SetupInProgress)
        {
            throw new InvalidOperationException($"Cannot generate nodes for a session that is in {session.Status} status. Only sessions in SetupInProgress can have nodes generated.");
        }

        // 2. Check if any nodes have already been checked (progression preserved)
        sw.Restart();
        var existingNodes = await _nodeRepository.GetBySessionIdAsync(request.SessionId);
        if (existingNodes.Any(n => n.State == "Checked"))
        {
            throw new InvalidOperationException("Cannot regenerate nodes for a session that already has checked nodes. Progression would be lost.");
        }
        _logger.LogInformation("[generate] CheckExistingNodes: {Ms}ms", sw.ElapsedMilliseconds);

        // 3. Update session with progression mode
        session.ProgressionMode = request.GameMode; // Assuming GameMode maps to ProgressionMode
        session.Location = new NetTopologySuite.Geometries.Point(request.CenterLon, request.CenterLat) { SRID = 4326 };
        session.Radius = (int)request.Radius;
        session.Mode = request.Mode;

        // 4. Fetch random nodes from PostGIS/OSM
        // We'll fetch a larger pool and then distribute them
        sw.Restart();
        var points = await _osmDiscoveryService.GetRandomNodesAsync(
            request.CenterLat,
            request.CenterLon,
            request.Radius,
            request.NodeCount * 2, // Fetch more to allow for filtering/distribution
            request.Mode,
            request.DensityBias);
        _logger.LogInformation("[generate] OsmDiscovery ({Count} points): {Ms}ms", points.Count, sw.ElapsedMilliseconds);

        if (points.Count < request.NodeCount)
            throw new Exception($"OSM Discovery returned only {points.Count} nodes, need {request.NodeCount}. Try increasing the radius.");

        var selectedPoints = DistributeNodes(points, request.CenterLat, request.CenterLon, request.Radius, request.NodeCount, request.GameMode);

        // 5. Delete existing nodes
        sw.Restart();
        await _nodeRepository.DeleteBySessionIdAsync(request.SessionId);
        _logger.LogInformation("[generate] DeleteExisting: {Ms}ms", sw.ElapsedMilliseconds);

        // 6. Bulk insert
        sw.Restart();
        var mapNodes = selectedPoints.Select((nodeData, i) => new MapNode
        {
            SessionId = session.Id,
            ApArrivalLocationId = 800000 + (2 * i + 1),
            ApPrecisionLocationId = 800000 + (2 * i + 2),
            OsmNodeId = $"osm-{request.SessionId}-{i + 1}",
            Name = $"Node {i + 1}",
            Location = new NetTopologySuite.Geometries.Point(nodeData.Point.Lon, nodeData.Point.Lat) { SRID = 4326 },
            State = nodeData.RegionTag == "Hub" ? "Available" : "Hidden",
            RegionTag = nodeData.RegionTag
        }).ToList();
        await _nodeRepository.CreateRangeAsync(mapNodes);
        _logger.LogInformation("[generate] BulkInsert ({Count} nodes): {Ms}ms", mapNodes.Count, sw.ElapsedMilliseconds);

        // 7. Update session status
        sw.Restart();
        session.Status = SessionStatus.Active;
        await _sessionRepository.UpdateAsync(session);
        _logger.LogInformation("[generate] UpdateSession: {Ms}ms", sw.ElapsedMilliseconds);

        _logger.LogInformation("[generate] TOTAL: {Ms}ms", total.ElapsedMilliseconds);
        return mapNodes.Count;
    }

    private class NodeDistribution
    {
        public DiscoveryPoint Point { get; set; } = null!;
        public string RegionTag { get; set; } = "Hub";
    }

    private List<NodeDistribution> DistributeNodes(List<DiscoveryPoint> points, double centerLat, double centerLon, double maxRadius, int targetCount, string mode)
    {
        var result = new List<NodeDistribution>();
        var hubRadius = maxRadius * 0.25;
        
        // 20% to Hub
        int hubTarget = (int)(targetCount * 0.20);
        var hubPoints = points
            .Where(p => CalculateDistance(centerLat, centerLon, p.Lat, p.Lon) <= hubRadius)
            .OrderBy(_ => Guid.NewGuid())
            .Take(hubTarget)
            .Select(p => new NodeDistribution { Point = p, RegionTag = "Hub" })
            .ToList();
        
        result.AddRange(hubPoints);

        var remainingPoints = points.Except(hubPoints.Select(h => h.Point)).ToList();
        int quadrantTarget = (targetCount - hubPoints.Count) / 4;

        if (mode.ToLower() == "quadrant")
        {
            // North: 315 to 45
            result.AddRange(FilterByAzimuth(remainingPoints, centerLat, centerLon, 315, 45, quadrantTarget, "North"));
            // East: 45 to 135
            result.AddRange(FilterByAzimuth(remainingPoints, centerLat, centerLon, 45, 135, quadrantTarget, "East"));
            // South: 135 to 225
            result.AddRange(FilterByAzimuth(remainingPoints, centerLat, centerLon, 135, 225, quadrantTarget, "South"));
            // West: 225 to 315
            result.AddRange(FilterByAzimuth(remainingPoints, centerLat, centerLon, 225, 315, quadrantTarget, "West"));
        }
        else
        {
            // For Radius or Free mode, just distribute the rest uniformly but tag them by quadrant anyway 
            // for potential "The Detour" consistency if needed, or just tag as "Outer".
            // Actually, let's keep region tags for "The Detour" logic.
            var outerNodes = remainingPoints
                .OrderBy(_ => Guid.NewGuid())
                .Take(targetCount - result.Count)
                .Select(p => {
                    double az = CalculateAzimuth(centerLat, centerLon, p.Lat, p.Lon);
                    string tag = GetRegionTag(az);
                    return new NodeDistribution { Point = p, RegionTag = tag };
                });
            result.AddRange(outerNodes);
        }

        return result;
    }

    private List<NodeDistribution> FilterByAzimuth(List<DiscoveryPoint> points, double lat1, double lon1, double startDeg, double endDeg, int count, string tag)
    {
        return points
            .Select(p => new { Point = p, Azimuth = CalculateAzimuth(lat1, lon1, p.Lat, p.Lon) })
            .Where(x => IsInWedge(x.Azimuth, startDeg, endDeg))
            .OrderBy(_ => Guid.NewGuid())
            .Take(count)
            .Select(x => new NodeDistribution { Point = x.Point, RegionTag = tag })
            .ToList();
    }

    private bool IsInWedge(double az, double start, double end)
    {
        if (start < end) return az >= start && az <= end;
        return az >= start || az <= end; // Wraps around North (0/360)
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

    private string GetRegionTag(double az)
    {
        if (IsInWedge(az, 315, 45)) return "North";
        if (IsInWedge(az, 45, 135)) return "East";
        if (IsInWedge(az, 135, 225)) return "South";
        return "West";
    }

    private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        double r = 6371000;
        double phi1 = lat1 * Math.PI / 180;
        double phi2 = lat2 * Math.PI / 180;
        double dphi = (lat2 - lat1) * Math.PI / 180;
        double dlambda = (lon2 - lon1) * Math.PI / 180;
        double a = Math.Sin(dphi / 2) * Math.Sin(dphi / 2) + Math.Cos(phi1) * Math.Cos(phi2) * Math.Sin(dlambda / 2) * Math.Sin(dlambda / 2);
        return r * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }
}
