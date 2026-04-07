using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bikeapelago.Api.Models;
using Bikeapelago.Api.Repositories;

namespace Bikeapelago.Api.Services;

public class NodeGenerationRequest
{
    public Guid SessionId { get; set; }
    public double CenterLat { get; set; }
    public double CenterLon { get; set; }
    public double Radius { get; set; }
    public int NodeCount { get; set; } = 50;
    public string Mode { get; set; } = "archipelago"; // or "singleplayer"
}

public class NodeGenerationService(
    IOsmDiscoveryService osmDiscoveryService,
    IMapNodeRepository nodeRepository,
    IGameSessionRepository sessionRepository)
{
    private readonly IOsmDiscoveryService _osmDiscoveryService = osmDiscoveryService;
    private readonly IMapNodeRepository _nodeRepository = nodeRepository;
    private readonly IGameSessionRepository _sessionRepository = sessionRepository;

    public async Task<int> GenerateNodesAsync(NodeGenerationRequest request)
    {
        Console.WriteLine($"[NodeGenerationService] Generating {request.NodeCount} nodes for session {request.SessionId} via osm-discovery-api.");

        // 1. Verify session exists
        var session = await _sessionRepository.GetByIdAsync(request.SessionId)
            ?? throw new Exception("Session not found");

        // 2. Fetch random nodes from the OSM Discovery API
        //    The API returns [{lat, lon}] — no IDs or names.
        //    We request more than needed so we have a buffer if the API returns fewer.
        var points = await _osmDiscoveryService.GetRandomNodesAsync(
            request.CenterLat,
            request.CenterLon,
            request.Radius,
            request.NodeCount);

        if (points.Count < request.NodeCount)
        {
            throw new Exception(
                $"OSM Discovery API returned only {points.Count} nodes, need {request.NodeCount}. " +
                "Try increasing the radius.");
        }

        // 3. Trim to exactly NodeCount (API may return extras if count < available)
        var selectedPoints = points.Take(request.NodeCount).ToList();

        // 4. Delete existing nodes for this session
        await _nodeRepository.DeleteBySessionIdAsync(request.SessionId);

        // 5. Create MapNodes in PocketBase
        int createdCount = 0;
        for (int i = 0; i < selectedPoints.Count; i++)
        {
            var point = selectedPoints[i];
            var mapNode = new MapNode
            {
                SessionId = session.Id,
                ApLocationId = 800000 + (i + 1), // Archipelago location ID mimic
                OsmNodeId = $"osm-{request.SessionId.ToString()}-{i + 1}", // synthetic ID
                Name = $"Node {i + 1}",
                Location = new NetTopologySuite.Geometries.Point(point.Lon, point.Lat) { SRID = 4326 },
                State = request.Mode == "singleplayer" && i < 3 ? "Available" : "Hidden"
            };

            await _nodeRepository.CreateAsync(mapNode);
            createdCount++;
        }

        // 6. Update session metadata and switch to Active
        session.Location = new NetTopologySuite.Geometries.Point(request.CenterLon, request.CenterLat) { SRID = 4326 };
        session.Radius = (int)request.Radius;
        session.Status = SessionStatus.Active;
        await _sessionRepository.UpdateAsync(session);

        return createdCount;
    }
}
