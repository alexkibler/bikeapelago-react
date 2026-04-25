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
    public string TransportMode { get; set; } = "bike"; // "bike" or "walk"
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
    SinglePlayerSeedGenerator singlePlayerSeedGenerator,
    ILogger<NodeGenerationService> logger) : INodeGenerationService
{
    private readonly IOsmDiscoveryService _osmDiscoveryService = osmDiscoveryService;
    private readonly IMapNodeRepository _nodeRepository = nodeRepository;
    private readonly IGameSessionRepository _sessionRepository = sessionRepository;
    private readonly SinglePlayerSeedGenerator _singlePlayerSeedGenerator = singlePlayerSeedGenerator;
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
        session.TransportMode = request.TransportMode;

        // 4. Distribution Strategy
        sw.Restart();
        var selectedPoints = await DistributeNodesAsync(request);
        _logger.LogInformation("[generate] Distribution: {Ms}ms", sw.ElapsedMilliseconds);

        // 5. Delete existing nodes
        sw.Restart();
        await _nodeRepository.DeleteBySessionIdAsync(request.SessionId);
        _logger.LogInformation("[generate] DeleteExisting: {Ms}ms", sw.ElapsedMilliseconds);

        // 6. Bulk insert
        sw.Restart();
        var mapNodes = selectedPoints.Select((nodeData, i) => new MapNode
        {
            SessionId = session.Id,
            ApArrivalLocationId = ItemDefinitions.StartId + (2 * i + 1),
            ApPrecisionLocationId = ItemDefinitions.StartId + (2 * i + 2),
            OsmNodeId = $"osm-{request.SessionId}-{i + 1}",
            Name = $"Node {i + 1}",
            Location = new NetTopologySuite.Geometries.Point(nodeData.Point.Lon, nodeData.Point.Lat) { SRID = 4326 },
            State = nodeData.RegionTag == "Hub" ? "Available" : "Hidden",
            RegionTag = nodeData.RegionTag
        }).ToList();

        if (session.ConnectionMode == "singleplayer")
        {
            _singlePlayerSeedGenerator.GenerateSeed(session, mapNodes);
        }

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

    private async Task<List<NodeDistribution>> DistributeNodesAsync(NodeGenerationRequest request)
    {
        var result = new List<NodeDistribution>();
        var hubRadius = request.Radius * 0.25;
        
        var seenPoints = new HashSet<(double, double)>();

        // 1. Hub Distribution (20%)
        int hubTarget = (int)(request.NodeCount * 0.20);
        var hubPoints = await _osmDiscoveryService.GetRandomNodesAsync(
            request.CenterLat, request.CenterLon, hubRadius, hubTarget, request.TransportMode, request.DensityBias);
        
        foreach (var p in hubPoints)
        {
            if (seenPoints.Add((p.Lat, p.Lon)))
            {
                // Strict check even for Hub-specific discovery
                double dist = CalculateDistance(request.CenterLat, request.CenterLon, p.Lat, p.Lon);
                result.Add(new NodeDistribution { Point = p, RegionTag = dist <= hubRadius ? "Hub" : GetRegionTag(CalculateAzimuth(request.CenterLat, request.CenterLon, p.Lat, p.Lon)) });
            }
        }

        // 2. Quadrant/Outer Distribution
        int remainingTarget = request.NodeCount - result.Count;
        
        if (request.GameMode.ToLower() == "quadrant")
        {
            int quadrantTarget = (int)Math.Ceiling(remainingTarget / 4.0);
            var quadrants = new[] 
            { 
                (315.0, 45.0, "North"), (45.0, 135.0, "East"), 
                (135.0, 225.0, "South"), (225.0, 315.0, "West") 
            };

            foreach (var q in quadrants)
            {
                var qPoints = await _osmDiscoveryService.GetRandomNodesInWedgeAsync(
                    request.CenterLat, request.CenterLon, request.Radius, q.Item1, q.Item2, quadrantTarget, request.TransportMode, request.DensityBias, hubRadius);
                
                foreach (var p in qPoints)
                {
                    if (!seenPoints.Add((p.Lat, p.Lon))) continue;
                    
                    // Final safety check: if it somehow fell into the Hub, tag it as Hub
                    double dist = CalculateDistance(request.CenterLat, request.CenterLon, p.Lat, p.Lon);
                    string tag = dist <= hubRadius ? "Hub" : q.Item3;
                    result.Add(new NodeDistribution { Point = p, RegionTag = tag });
                }
            }
        }
        else
        {
            var outerPoints = await _osmDiscoveryService.GetRandomNodesAsync(
                request.CenterLat, request.CenterLon, request.Radius, remainingTarget + 10, request.TransportMode, request.DensityBias);
            
            foreach (var p in outerPoints)
            {
                if (!seenPoints.Add((p.Lat, p.Lon))) continue;
                if (result.Count >= request.NodeCount) break;

                double dist = CalculateDistance(request.CenterLat, request.CenterLon, p.Lat, p.Lon);
                if (dist <= hubRadius)
                {
                    result.Add(new NodeDistribution { Point = p, RegionTag = "Hub" });
                }
                else
                {
                    double az = CalculateAzimuth(request.CenterLat, request.CenterLon, p.Lat, p.Lon);
                    result.Add(new NodeDistribution { Point = p, RegionTag = GetRegionTag(az) });
                }
            }
        }

        return result.Take(request.NodeCount).ToList();
    }

    private string GetRegionTag(double az)
    {
        if (IsInWedge(az, 315, 45)) return "North";
        if (IsInWedge(az, 45, 135)) return "East";
        if (IsInWedge(az, 135, 225)) return "South";
        return "West";
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
