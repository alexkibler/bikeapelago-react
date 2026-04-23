using Bikeapelago.Api.Models;
using Bikeapelago.Api.Repositories;
using NetTopologySuite.Geometries;

namespace Bikeapelago.Api.Services;

/// <inheritdoc />
public class RouteBuilderService(
    IGameSessionRepository sessionRepository,
    IMapNodeRepository nodeRepository,
    IMapboxRoutingService mapboxRoutingService) : IRouteBuilderService
{
    private readonly IGameSessionRepository _sessionRepository = sessionRepository;
    private readonly IMapNodeRepository _nodeRepository = nodeRepository;
    private readonly IMapboxRoutingService _mapboxRoutingService = mapboxRoutingService;

    /// <inheritdoc />
    public async Task<RouteBuilderResult> BuildRouteAsync(Guid sessionId, RouteWaypointsRequest request)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session == null)
            return RouteBuilderResult.Fail("Session not found");

        // ── 1. Resolve origin ─────────────────────────────────────────────────
        Point originPoint;
        if (request.CustomOrigin != null)
        {
            originPoint = new Point(request.CustomOrigin.Longitude, request.CustomOrigin.Latitude) { SRID = 4326 };
        }
        else if (session.Location != null)
        {
            originPoint = session.Location;
        }
        else
        {
            return RouteBuilderResult.Fail("No origin provided and session has no location");
        }

        // ── 2. Resolve target nodes ───────────────────────────────────────────
        var allNodes = await _nodeRepository.GetBySessionIdAsync(sessionId);
        List<MapNode> targetNodes;

        if (request.NodeIds.Count > 0)
        {
            targetNodes = allNodes.Where(n => request.NodeIds.Contains(n.Id)).ToList();
            if (targetNodes.Count == 0)
                return RouteBuilderResult.Fail("None of the specified node IDs were found for this session");
        }
        else
        {
            targetNodes = allNodes.Where(n => n.State == "Available").ToList();
            if (targetNodes.Count == 0)
                return RouteBuilderResult.Fail("No available nodes to route to");
        }

        // ── 3. Optimize route ─────────────────────────────────────────────────
        var result = await _mapboxRoutingService.RouteToMultipleNodesAsync(originPoint, targetNodes, request.Profile);
        if (!result.Success)
            return RouteBuilderResult.Fail(result.Error ?? "Routing failed");

        // ── 4. Elevation ──────────────────────────────────────────────────────
        var elevationGain = await _mapboxRoutingService.CalculateElevationGainAsync(result.Geometry);

        // ── 5. Reconstruct ordered node list ──────────────────────────────────
        var orderedNodes = result.OrderedNodeIds
            .Select(nid => targetNodes.FirstOrDefault(n => n.Id == nid))
            .Where(n => n != null)
            .Cast<MapNode>()
            .ToList();

        // ── 6. Persist snapped locations ──────────────────────────────────────
        if (result.SnappedLocations.Count > 0)
        {
            var nodesToUpdate = new List<MapNode>();
            foreach (var node in orderedNodes)
            {
                if (result.SnappedLocations.TryGetValue(node.Id, out var snapped) && snapped.Count >= 2)
                {
                    node.Location = new Point(snapped[0], snapped[1]) { SRID = 4326 };
                    nodesToUpdate.Add(node);
                }
            }
            if (nodesToUpdate.Count > 0)
                await _nodeRepository.UpdateRangeAsync(nodesToUpdate);
        }

        // ── 7. Generate GPX ───────────────────────────────────────────────────
        var gpxString = _mapboxRoutingService.GenerateGpx(
            result.Geometry, orderedNodes, request.TurnByTurn, result.SnappedLocations);

        var snappedNodeLocations = result.SnappedLocations
            .ToDictionary(
                kvp => kvp.Key.ToString(),
                kvp => new SnappedNodeLocation { Lon = kvp.Value[0], Lat = kvp.Value[1] });

        return new RouteBuilderResult
        {
            Success = true,
            Geometry = result.Geometry,
            OrderedNodeIds = result.OrderedNodeIds,
            TotalDistanceMeters = result.TotalDistanceMeters,
            TotalDurationSeconds = result.TotalDurationSeconds,
            ElevationGain = elevationGain,
            GpxString = gpxString,
            SnappedNodeLocations = snappedNodeLocations,
        };
    }
}
