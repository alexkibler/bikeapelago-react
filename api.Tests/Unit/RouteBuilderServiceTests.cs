using Bikeapelago.Api.Models;
using Bikeapelago.Api.Repositories;
using Bikeapelago.Api.Services;
using Moq;
using NetTopologySuite.Geometries;
using Xunit;

namespace Bikeapelago.Api.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="RouteBuilderService"/>.
/// All external dependencies are mocked; no network or database calls are made.
/// </summary>
public class RouteBuilderServiceTests
{
    // ── Shared test helpers ───────────────────────────────────────────────────

    private readonly Mock<IGameSessionRepository> _sessionRepo = new();
    private readonly Mock<IMapNodeRepository> _nodeRepo = new();
    private readonly Mock<IMapboxRoutingService> _mapbox = new();

    private RouteBuilderService CreateService() =>
        new(_sessionRepo.Object, _nodeRepo.Object, _mapbox.Object);

    private static GameSession SessionAt(Guid id, double lat = 40.44, double lon = -79.99) =>
        new() { Id = id, Location = new Point(lon, lat) { SRID = 4326 } };

    private static MapNode AvailableNode(Guid? id = null, double lat = 40.44, double lon = -79.99) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            State = "Available",
            Name = "Node",
            Location = new Point(lon, lat) { SRID = 4326 }
        };

    private static RouteWaypointsRequest DefaultRequest() =>
        new() { Profile = "cycling", TurnByTurn = true };

    private void SetupSuccessfulMapbox(Guid nodeId, double lat, double lon) =>
        _mapbox
            .Setup(m => m.RouteToMultipleNodesAsync(
                It.IsAny<Point>(), It.IsAny<List<MapNode>>(), It.IsAny<string>()))
            .ReturnsAsync(new OptimizedRouteResult
            {
                Success = true,
                Geometry = [[lon, lat]],
                OrderedNodeIds = [nodeId],
                SnappedLocations = [],
                TotalDistanceMeters = 1000,
                TotalDurationSeconds = 300
            });

    // ── Session not found ─────────────────────────────────────────────────────

    [Fact]
    public async Task BuildRouteAsync_SessionNotFound_ReturnsFailure()
    {
        var sessionId = Guid.NewGuid();
        _sessionRepo.Setup(r => r.GetByIdAsync(sessionId)).ReturnsAsync((GameSession?)null);

        var result = await CreateService().BuildRouteAsync(sessionId, DefaultRequest());

        Assert.False(result.Success);
        Assert.Equal("Session not found", result.Error);
    }

    // ── Origin resolution ─────────────────────────────────────────────────────

    [Fact]
    public async Task BuildRouteAsync_NoCustomOrigin_UsesSessionLocation()
    {
        var sessionId = Guid.NewGuid();
        var nodeId = Guid.NewGuid();
        var session = SessionAt(sessionId, lat: 40.44, lon: -79.99);

        _sessionRepo.Setup(r => r.GetByIdAsync(sessionId)).ReturnsAsync(session);
        _nodeRepo.Setup(r => r.GetBySessionIdAsync(sessionId))
            .ReturnsAsync([AvailableNode(nodeId)]);
        SetupSuccessfulMapbox(nodeId, 40.44, -79.99);
        _mapbox.Setup(m => m.CalculateElevationGainAsync(It.IsAny<List<List<double>>>())).ReturnsAsync(0);
        _mapbox.Setup(m => m.GenerateGpx(It.IsAny<List<List<double>>>(), It.IsAny<List<MapNode>>(),
            It.IsAny<bool>(), It.IsAny<Dictionary<Guid, List<double>>?>())).Returns("<gpx/>");

        await CreateService().BuildRouteAsync(sessionId, DefaultRequest());

        // Verify the routing engine was called with the session's location
        _mapbox.Verify(m => m.RouteToMultipleNodesAsync(
            It.Is<Point>(p => Math.Abs(p.Y - 40.44) < 1e-6 && Math.Abs(p.X - -79.99) < 1e-6),
            It.IsAny<List<MapNode>>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task BuildRouteAsync_WithCustomOrigin_UsesCustomOrigin()
    {
        var sessionId = Guid.NewGuid();
        var nodeId = Guid.NewGuid();
        var session = SessionAt(sessionId, lat: 40.00, lon: -80.00); // session centre far away

        var request = DefaultRequest();
        request.CustomOrigin = new MapboxCoordinate(-75.00, 39.00); // custom start

        _sessionRepo.Setup(r => r.GetByIdAsync(sessionId)).ReturnsAsync(session);
        _nodeRepo.Setup(r => r.GetBySessionIdAsync(sessionId))
            .ReturnsAsync([AvailableNode(nodeId)]);
        SetupSuccessfulMapbox(nodeId, 39.00, -75.00);
        _mapbox.Setup(m => m.CalculateElevationGainAsync(It.IsAny<List<List<double>>>())).ReturnsAsync(0);
        _mapbox.Setup(m => m.GenerateGpx(It.IsAny<List<List<double>>>(), It.IsAny<List<MapNode>>(),
            It.IsAny<bool>(), It.IsAny<Dictionary<Guid, List<double>>?>())).Returns("<gpx/>");

        await CreateService().BuildRouteAsync(sessionId, request);

        // Must use the custom origin, not the session centre
        _mapbox.Verify(m => m.RouteToMultipleNodesAsync(
            It.Is<Point>(p => Math.Abs(p.Y - 39.00) < 1e-6 && Math.Abs(p.X - -75.00) < 1e-6),
            It.IsAny<List<MapNode>>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task BuildRouteAsync_NoCustomOriginAndNoSessionLocation_ReturnsFailure()
    {
        var sessionId = Guid.NewGuid();
        _sessionRepo.Setup(r => r.GetByIdAsync(sessionId))
            .ReturnsAsync(new GameSession { Id = sessionId, Location = null });

        var result = await CreateService().BuildRouteAsync(sessionId, DefaultRequest());

        Assert.False(result.Success);
        Assert.Contains("No origin", result.Error);
    }

    // ── Target node resolution ────────────────────────────────────────────────

    [Fact]
    public async Task BuildRouteAsync_EmptyNodeIds_RoutesToAllAvailableNodes()
    {
        var sessionId = Guid.NewGuid();
        var nodeA = AvailableNode();
        var nodeB = AvailableNode();
        var checkedNode = new MapNode { Id = Guid.NewGuid(), State = "Checked", Name = "Done" };

        _sessionRepo.Setup(r => r.GetByIdAsync(sessionId)).ReturnsAsync(SessionAt(sessionId));
        _nodeRepo.Setup(r => r.GetBySessionIdAsync(sessionId))
            .ReturnsAsync([nodeA, nodeB, checkedNode]);

        List<MapNode>? capturedTargets = null;
        _mapbox
            .Setup(m => m.RouteToMultipleNodesAsync(It.IsAny<Point>(), It.IsAny<List<MapNode>>(), It.IsAny<string>()))
            .Callback<Point, List<MapNode>, string>((_, nodes, _) => capturedTargets = nodes)
            .ReturnsAsync(new OptimizedRouteResult
            {
                Success = true,
                Geometry = [],
                OrderedNodeIds = [nodeA.Id, nodeB.Id],
                SnappedLocations = [],
            });
        _mapbox.Setup(m => m.CalculateElevationGainAsync(It.IsAny<List<List<double>>>())).ReturnsAsync(0);
        _mapbox.Setup(m => m.GenerateGpx(It.IsAny<List<List<double>>>(), It.IsAny<List<MapNode>>(),
            It.IsAny<bool>(), It.IsAny<Dictionary<Guid, List<double>>?>())).Returns("<gpx/>");

        await CreateService().BuildRouteAsync(sessionId, DefaultRequest());

        Assert.NotNull(capturedTargets);
        Assert.Equal(2, capturedTargets!.Count);
        Assert.All(capturedTargets, n => Assert.Equal("Available", n.State));
        Assert.DoesNotContain(capturedTargets, n => n.Id == checkedNode.Id);
    }

    [Fact]
    public async Task BuildRouteAsync_WithSpecificNodeIds_RoutesToOnlyThoseNodes()
    {
        var sessionId = Guid.NewGuid();
        var nodeA = AvailableNode();
        var nodeB = AvailableNode();
        var nodeC = AvailableNode(); // not selected

        _sessionRepo.Setup(r => r.GetByIdAsync(sessionId)).ReturnsAsync(SessionAt(sessionId));
        _nodeRepo.Setup(r => r.GetBySessionIdAsync(sessionId))
            .ReturnsAsync([nodeA, nodeB, nodeC]);

        List<MapNode>? capturedTargets = null;
        _mapbox
            .Setup(m => m.RouteToMultipleNodesAsync(It.IsAny<Point>(), It.IsAny<List<MapNode>>(), It.IsAny<string>()))
            .Callback<Point, List<MapNode>, string>((_, nodes, _) => capturedTargets = nodes)
            .ReturnsAsync(new OptimizedRouteResult
            {
                Success = true, Geometry = [],
                OrderedNodeIds = [nodeA.Id, nodeB.Id],
                SnappedLocations = [],
            });
        _mapbox.Setup(m => m.CalculateElevationGainAsync(It.IsAny<List<List<double>>>())).ReturnsAsync(0);
        _mapbox.Setup(m => m.GenerateGpx(It.IsAny<List<List<double>>>(), It.IsAny<List<MapNode>>(),
            It.IsAny<bool>(), It.IsAny<Dictionary<Guid, List<double>>?>())).Returns("<gpx/>");

        var request = DefaultRequest();
        request.NodeIds = [nodeA.Id, nodeB.Id]; // only A and B

        await CreateService().BuildRouteAsync(sessionId, request);

        Assert.NotNull(capturedTargets);
        Assert.Equal(2, capturedTargets!.Count);
        Assert.Contains(capturedTargets, n => n.Id == nodeA.Id);
        Assert.Contains(capturedTargets, n => n.Id == nodeB.Id);
        Assert.DoesNotContain(capturedTargets, n => n.Id == nodeC.Id);
    }

    [Fact]
    public async Task BuildRouteAsync_NodeIdsProvidedButNoneFound_ReturnsFailure()
    {
        var sessionId = Guid.NewGuid();
        _sessionRepo.Setup(r => r.GetByIdAsync(sessionId)).ReturnsAsync(SessionAt(sessionId));
        _nodeRepo.Setup(r => r.GetBySessionIdAsync(sessionId)).ReturnsAsync([AvailableNode()]);

        var request = DefaultRequest();
        request.NodeIds = [Guid.NewGuid(), Guid.NewGuid()]; // IDs that don't exist

        var result = await CreateService().BuildRouteAsync(sessionId, request);

        Assert.False(result.Success);
        Assert.Contains("None of the specified node IDs", result.Error);
    }

    [Fact]
    public async Task BuildRouteAsync_NoNodeIdsAndNoAvailableNodes_ReturnsFailure()
    {
        var sessionId = Guid.NewGuid();
        _sessionRepo.Setup(r => r.GetByIdAsync(sessionId)).ReturnsAsync(SessionAt(sessionId));
        _nodeRepo.Setup(r => r.GetBySessionIdAsync(sessionId))
            .ReturnsAsync([new MapNode { Id = Guid.NewGuid(), State = "Hidden" }]);

        var result = await CreateService().BuildRouteAsync(sessionId, DefaultRequest());

        Assert.False(result.Success);
        Assert.Contains("No available nodes", result.Error);
    }

    // ── Routing engine failure ────────────────────────────────────────────────

    [Fact]
    public async Task BuildRouteAsync_RoutingServiceFails_ReturnsFailure()
    {
        var sessionId = Guid.NewGuid();
        _sessionRepo.Setup(r => r.GetByIdAsync(sessionId)).ReturnsAsync(SessionAt(sessionId));
        _nodeRepo.Setup(r => r.GetBySessionIdAsync(sessionId)).ReturnsAsync([AvailableNode()]);
        _mapbox
            .Setup(m => m.RouteToMultipleNodesAsync(It.IsAny<Point>(), It.IsAny<List<MapNode>>(), It.IsAny<string>()))
            .ReturnsAsync(new OptimizedRouteResult { Success = false, Error = "OSRM unavailable" });

        var result = await CreateService().BuildRouteAsync(sessionId, DefaultRequest());

        Assert.False(result.Success);
        Assert.Equal("OSRM unavailable", result.Error);
    }

    // ── Snapped location persistence ──────────────────────────────────────────

    [Fact]
    public async Task BuildRouteAsync_WithSnappedLocations_PersistsSnappedPositions()
    {
        var sessionId = Guid.NewGuid();
        var nodeId = Guid.NewGuid();
        const double snappedLat = 40.4405;
        const double snappedLon = -79.9952;

        var node = AvailableNode(nodeId, lat: 40.44, lon: -79.99);

        _sessionRepo.Setup(r => r.GetByIdAsync(sessionId)).ReturnsAsync(SessionAt(sessionId));
        _nodeRepo.Setup(r => r.GetBySessionIdAsync(sessionId)).ReturnsAsync([node]);
        _nodeRepo.Setup(r => r.UpdateRangeAsync(It.IsAny<IEnumerable<MapNode>>())).Returns(Task.CompletedTask);

        _mapbox
            .Setup(m => m.RouteToMultipleNodesAsync(It.IsAny<Point>(), It.IsAny<List<MapNode>>(), It.IsAny<string>()))
            .ReturnsAsync(new OptimizedRouteResult
            {
                Success = true,
                Geometry = [[snappedLon, snappedLat]],
                OrderedNodeIds = [nodeId],
                SnappedLocations = new Dictionary<Guid, List<double>>
                {
                    [nodeId] = [snappedLon, snappedLat]
                },
                TotalDistanceMeters = 1000,
                TotalDurationSeconds = 300
            });
        _mapbox.Setup(m => m.CalculateElevationGainAsync(It.IsAny<List<List<double>>>())).ReturnsAsync(50.0);
        _mapbox.Setup(m => m.GenerateGpx(It.IsAny<List<List<double>>>(), It.IsAny<List<MapNode>>(),
            It.IsAny<bool>(), It.IsAny<Dictionary<Guid, List<double>>?>())).Returns("<gpx/>");

        var result = await CreateService().BuildRouteAsync(sessionId, DefaultRequest());

        Assert.True(result.Success);
        _nodeRepo.Verify(r => r.UpdateRangeAsync(It.Is<IEnumerable<MapNode>>(nodes =>
            nodes.Any(n =>
                n.Id == nodeId &&
                Math.Abs(n.Lat!.Value - snappedLat) < 1e-6 &&
                Math.Abs(n.Lon!.Value - snappedLon) < 1e-6))),
            Times.Once, "Node should be persisted at the snapped road position");
    }

    [Fact]
    public async Task BuildRouteAsync_WithNoSnappedLocations_DoesNotCallUpdateRange()
    {
        var sessionId = Guid.NewGuid();
        var nodeId = Guid.NewGuid();

        _sessionRepo.Setup(r => r.GetByIdAsync(sessionId)).ReturnsAsync(SessionAt(sessionId));
        _nodeRepo.Setup(r => r.GetBySessionIdAsync(sessionId)).ReturnsAsync([AvailableNode(nodeId)]);
        _mapbox
            .Setup(m => m.RouteToMultipleNodesAsync(It.IsAny<Point>(), It.IsAny<List<MapNode>>(), It.IsAny<string>()))
            .ReturnsAsync(new OptimizedRouteResult
            {
                Success = true, Geometry = [],
                OrderedNodeIds = [nodeId],
                SnappedLocations = [], // nothing snapped
            });
        _mapbox.Setup(m => m.CalculateElevationGainAsync(It.IsAny<List<List<double>>>())).ReturnsAsync(0);
        _mapbox.Setup(m => m.GenerateGpx(It.IsAny<List<List<double>>>(), It.IsAny<List<MapNode>>(),
            It.IsAny<bool>(), It.IsAny<Dictionary<Guid, List<double>>?>())).Returns("<gpx/>");

        await CreateService().BuildRouteAsync(sessionId, DefaultRequest());

        _nodeRepo.Verify(r => r.UpdateRangeAsync(It.IsAny<IEnumerable<MapNode>>()), Times.Never,
            "UpdateRangeAsync should not be called when nothing was snapped");
    }

    // ── Successful result shape ───────────────────────────────────────────────

    [Fact]
    public async Task BuildRouteAsync_Success_ReturnsCorrectDistanceAndElevation()
    {
        var sessionId = Guid.NewGuid();
        var nodeId = Guid.NewGuid();

        _sessionRepo.Setup(r => r.GetByIdAsync(sessionId)).ReturnsAsync(SessionAt(sessionId));
        _nodeRepo.Setup(r => r.GetBySessionIdAsync(sessionId)).ReturnsAsync([AvailableNode(nodeId)]);
        _mapbox
            .Setup(m => m.RouteToMultipleNodesAsync(It.IsAny<Point>(), It.IsAny<List<MapNode>>(), It.IsAny<string>()))
            .ReturnsAsync(new OptimizedRouteResult
            {
                Success = true, Geometry = [],
                OrderedNodeIds = [nodeId],
                SnappedLocations = [],
                TotalDistanceMeters = 8500,
                TotalDurationSeconds = 2100
            });
        _mapbox.Setup(m => m.CalculateElevationGainAsync(It.IsAny<List<List<double>>>())).ReturnsAsync(123.4);
        _mapbox.Setup(m => m.GenerateGpx(It.IsAny<List<List<double>>>(), It.IsAny<List<MapNode>>(),
            It.IsAny<bool>(), It.IsAny<Dictionary<Guid, List<double>>?>())).Returns("<gpx>...</gpx>");

        var result = await CreateService().BuildRouteAsync(sessionId, DefaultRequest());

        Assert.True(result.Success);
        Assert.Equal(8500, result.TotalDistanceMeters);
        Assert.Equal(2100, result.TotalDurationSeconds);
        Assert.Equal(123.4, result.ElevationGain, precision: 1);
        Assert.Equal("<gpx>...</gpx>", result.GpxString);
    }

    [Fact]
    public async Task BuildRouteAsync_Success_SnappedNodeLocationsAreIncludedInResult()
    {
        var sessionId = Guid.NewGuid();
        var nodeId = Guid.NewGuid();

        _sessionRepo.Setup(r => r.GetByIdAsync(sessionId)).ReturnsAsync(SessionAt(sessionId));
        _nodeRepo.Setup(r => r.GetBySessionIdAsync(sessionId)).ReturnsAsync([AvailableNode(nodeId)]);
        _nodeRepo.Setup(r => r.UpdateRangeAsync(It.IsAny<IEnumerable<MapNode>>())).Returns(Task.CompletedTask);
        _mapbox
            .Setup(m => m.RouteToMultipleNodesAsync(It.IsAny<Point>(), It.IsAny<List<MapNode>>(), It.IsAny<string>()))
            .ReturnsAsync(new OptimizedRouteResult
            {
                Success = true, Geometry = [],
                OrderedNodeIds = [nodeId],
                SnappedLocations = new Dictionary<Guid, List<double>>
                {
                    [nodeId] = [-79.99, 40.55]
                },
            });
        _mapbox.Setup(m => m.CalculateElevationGainAsync(It.IsAny<List<List<double>>>())).ReturnsAsync(0);
        _mapbox.Setup(m => m.GenerateGpx(It.IsAny<List<List<double>>>(), It.IsAny<List<MapNode>>(),
            It.IsAny<bool>(), It.IsAny<Dictionary<Guid, List<double>>?>())).Returns("<gpx/>");

        var result = await CreateService().BuildRouteAsync(sessionId, DefaultRequest());

        Assert.True(result.Success);
        Assert.True(result.SnappedNodeLocations.ContainsKey(nodeId.ToString()));
        var snapped = result.SnappedNodeLocations[nodeId.ToString()];
        Assert.Equal(-79.99, snapped.Lon, precision: 4);
        Assert.Equal(40.55,  snapped.Lat, precision: 4);
    }

    [Fact]
    public async Task BuildRouteAsync_WithExplicitNodeIds_IncludesNodesRegardlessOfState()
    {
        var sessionId = Guid.NewGuid();
        var nodeA = new MapNode { Id = Guid.NewGuid(), State = "Checked", Name = "A" };
        var nodeB = new MapNode { Id = Guid.NewGuid(), State = "Hidden", Name = "B" };

        _sessionRepo.Setup(r => r.GetByIdAsync(sessionId)).ReturnsAsync(SessionAt(sessionId));
        _nodeRepo.Setup(r => r.GetBySessionIdAsync(sessionId))
            .ReturnsAsync([nodeA, nodeB]);

        List<MapNode>? capturedTargets = null;
        _mapbox
            .Setup(m => m.RouteToMultipleNodesAsync(It.IsAny<Point>(), It.IsAny<List<MapNode>>(), It.IsAny<string>()))
            .Callback<Point, List<MapNode>, string>((_, nodes, _) => capturedTargets = nodes)
            .ReturnsAsync(new OptimizedRouteResult { Success = true, OrderedNodeIds = [nodeA.Id, nodeB.Id] });
        _mapbox.Setup(m => m.CalculateElevationGainAsync(It.IsAny<List<List<double>>>())).ReturnsAsync(0);
        _mapbox.Setup(m => m.GenerateGpx(It.IsAny<List<List<double>>>(), It.IsAny<List<MapNode>>(),
            It.IsAny<bool>(), It.IsAny<Dictionary<Guid, List<double>>?>())).Returns("<gpx/>");

        var request = DefaultRequest();
        request.NodeIds = [nodeA.Id, nodeB.Id];

        await CreateService().BuildRouteAsync(sessionId, request);

        Assert.NotNull(capturedTargets);
        Assert.Equal(2, capturedTargets!.Count);
        Assert.Contains(capturedTargets, n => n.Id == nodeA.Id);
        Assert.Contains(capturedTargets, n => n.Id == nodeB.Id);
    }

    [Fact]
    public async Task BuildRouteAsync_PassesProfileToMapboxService()
    {
        var sessionId = Guid.NewGuid();
        var node = AvailableNode();
        _sessionRepo.Setup(r => r.GetByIdAsync(sessionId)).ReturnsAsync(SessionAt(sessionId));
        _nodeRepo.Setup(r => r.GetBySessionIdAsync(sessionId)).ReturnsAsync([node]);

        _mapbox
            .Setup(m => m.RouteToMultipleNodesAsync(It.IsAny<Point>(), It.IsAny<List<MapNode>>(), "walking"))
            .ReturnsAsync(new OptimizedRouteResult { Success = true, OrderedNodeIds = [node.Id] });
        _mapbox.Setup(m => m.CalculateElevationGainAsync(It.IsAny<List<List<double>>>())).ReturnsAsync(0);
        _mapbox.Setup(m => m.GenerateGpx(It.IsAny<List<List<double>>>(), It.IsAny<List<MapNode>>(),
            It.IsAny<bool>(), It.IsAny<Dictionary<Guid, List<double>>?>())).Returns("<gpx/>");

        var request = DefaultRequest();
        request.Profile = "walking";

        var result = await CreateService().BuildRouteAsync(sessionId, request);

        Assert.True(result.Success);
        _mapbox.Verify(m => m.RouteToMultipleNodesAsync(It.IsAny<Point>(), It.IsAny<List<MapNode>>(), "walking"), Times.Once);
    }
}
