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

        // 3. Fetch random nodes from PostGIS
        sw.Restart();
        var points = await _osmDiscoveryService.GetRandomNodesAsync(
            request.CenterLat,
            request.CenterLon,
            request.Radius,
            request.NodeCount,
            request.Mode,
            request.DensityBias);
        _logger.LogInformation("[generate] OsmDiscovery ({Count} points): {Ms}ms", points.Count, sw.ElapsedMilliseconds);

        if (points.Count < request.NodeCount)
            throw new Exception($"OSM Discovery returned only {points.Count} nodes, need {request.NodeCount}. Try increasing the radius.");

        var selectedPoints = points.Take(request.NodeCount).OrderBy(_ => Guid.NewGuid()).ToList();

        // 4. Delete existing nodes
        sw.Restart();
        await _nodeRepository.DeleteBySessionIdAsync(request.SessionId);
        _logger.LogInformation("[generate] DeleteExisting: {Ms}ms", sw.ElapsedMilliseconds);

        // 5. Bulk insert
        sw.Restart();
        var mapNodes = selectedPoints.Select((point, i) => new MapNode
        {
            SessionId = session.Id,
            ApLocationId = 800000 + (i + 1),
            OsmNodeId = $"osm-{request.SessionId}-{i + 1}",
            Name = $"Node {i + 1}",
            Location = new NetTopologySuite.Geometries.Point(point.Lon, point.Lat) { SRID = 4326 },
            State = i < 3 ? "Available" : "Hidden"
        }).ToList();
        await _nodeRepository.CreateRangeAsync(mapNodes);
        _logger.LogInformation("[generate] BulkInsert ({Count} nodes): {Ms}ms", mapNodes.Count, sw.ElapsedMilliseconds);

        // 6. Update session
        sw.Restart();
        session.Location = new NetTopologySuite.Geometries.Point(request.CenterLon, request.CenterLat) { SRID = 4326 };
        session.Radius = (int)request.Radius;
        session.Mode = request.Mode;
        session.Status = SessionStatus.Active;
        await _sessionRepository.UpdateAsync(session);
        _logger.LogInformation("[generate] UpdateSession: {Ms}ms", sw.ElapsedMilliseconds);

        _logger.LogInformation("[generate] TOTAL: {Ms}ms", total.ElapsedMilliseconds);
        return mapNodes.Count;
    }
}
