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
}

public class NodeGenerationService(
    IOsmDiscoveryService osmDiscoveryService,
    IMapNodeRepository nodeRepository,
    IGameSessionRepository sessionRepository,
    ILogger<NodeGenerationService> logger)
{
    private readonly IOsmDiscoveryService _osmDiscoveryService = osmDiscoveryService;
    private readonly IMapNodeRepository _nodeRepository = nodeRepository;
    private readonly IGameSessionRepository _sessionRepository = sessionRepository;
    private readonly ILogger<NodeGenerationService> _logger = logger;

    public async Task<int> GenerateNodesAsync(NodeGenerationRequest request)
    {
        var total = Stopwatch.StartNew();
        var sw = Stopwatch.StartNew();

        // 1. Verify session exists
        var session = await _sessionRepository.GetByIdAsync(request.SessionId)
            ?? throw new Exception("Session not found");
        _logger.LogInformation("[generate] GetSession: {Ms}ms", sw.ElapsedMilliseconds);

        // 2. Fetch random nodes from PostGIS
        sw.Restart();
        var points = await _osmDiscoveryService.GetRandomNodesAsync(
            request.CenterLat,
            request.CenterLon,
            request.Radius,
            request.NodeCount,
            request.Mode);
        _logger.LogInformation("[generate] OsmDiscovery ({Count} points): {Ms}ms", points.Count, sw.ElapsedMilliseconds);

        if (points.Count < request.NodeCount)
            throw new Exception($"OSM Discovery returned only {points.Count} nodes, need {request.NodeCount}. Try increasing the radius.");

        var selectedPoints = points.Take(request.NodeCount).ToList();

        // 3. Delete existing nodes
        sw.Restart();
        await _nodeRepository.DeleteBySessionIdAsync(request.SessionId);
        _logger.LogInformation("[generate] DeleteExisting: {Ms}ms", sw.ElapsedMilliseconds);

        // 4. Bulk insert
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

        // 5. Update session
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
