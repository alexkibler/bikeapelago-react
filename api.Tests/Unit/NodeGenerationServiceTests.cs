using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bikeapelago.Api.Models;
using Bikeapelago.Api.Repositories;
using Bikeapelago.Api.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using NetTopologySuite.Geometries;

namespace Bikeapelago.Api.Tests.Unit;

public class NodeGenerationServiceTests
{
    private readonly Mock<IOsmDiscoveryService> _osmDiscoveryServiceMock;
    private readonly Mock<IMapNodeRepository> _nodeRepositoryMock;
    private readonly Mock<IGameSessionRepository> _sessionRepositoryMock;
    private readonly Mock<ILogger<NodeGenerationService>> _loggerMock;
    private readonly NodeGenerationService _service;

    public NodeGenerationServiceTests()
    {
        _osmDiscoveryServiceMock = new Mock<IOsmDiscoveryService>();
        _nodeRepositoryMock = new Mock<IMapNodeRepository>();
        _sessionRepositoryMock = new Mock<IGameSessionRepository>();
        _loggerMock = new Mock<ILogger<NodeGenerationService>>();

        _service = new NodeGenerationService(
            _osmDiscoveryServiceMock.Object,
            _nodeRepositoryMock.Object,
            _sessionRepositoryMock.Object,
            new SinglePlayerSeedGenerator(Mock.Of<ILogger<SinglePlayerSeedGenerator>>()),
            _loggerMock.Object);
    }

    [Fact]
    public async Task GenerateNodesAsync_ShouldFail_WhenSessionIsAlreadyActive()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var session = new GameSession
        {
            Id = sessionId,
            Status = SessionStatus.Active
        };

        _sessionRepositoryMock.Setup(r => r.GetByIdAsync(sessionId))
            .ReturnsAsync(session);

        var request = new NodeGenerationRequest
        {
            SessionId = sessionId,
            CenterLat = 40.0,
            CenterLon = -74.0,
            Radius = 1000,
            NodeCount = 10
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.GenerateNodesAsync(request));
        Assert.Contains("status", exception.Message);
        Assert.Contains("SetupInProgress", exception.Message);
        
        // Verify delete was NOT called
        _nodeRepositoryMock.Verify(r => r.DeleteBySessionIdAsync(sessionId), Times.Never);
    }

    [Fact]
    public async Task GenerateNodesAsync_ShouldFail_WhenNodesAreAlreadyChecked()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var session = new GameSession
        {
            Id = sessionId,
            Status = SessionStatus.SetupInProgress // Status might be okay, but nodes are checked
        };

        var existingNodes = new List<MapNode>
        {
            new MapNode { SessionId = sessionId, State = "Checked" },
            new MapNode { SessionId = sessionId, State = "Available" }
        };

        _sessionRepositoryMock.Setup(r => r.GetByIdAsync(sessionId))
            .ReturnsAsync(session);

        _nodeRepositoryMock.Setup(r => r.GetBySessionIdAsync(sessionId))
            .ReturnsAsync(existingNodes);

        var request = new NodeGenerationRequest
        {
            SessionId = sessionId,
            CenterLat = 40.0,
            CenterLon = -74.0,
            Radius = 1000,
            NodeCount = 10
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.GenerateNodesAsync(request));
        Assert.Contains("already has checked nodes", exception.Message);

        // Verify delete was NOT called
        _nodeRepositoryMock.Verify(r => r.DeleteBySessionIdAsync(sessionId), Times.Never);
    }

    [Fact]
    public async Task GenerateNodesAsync_QuadrantMode_ShouldCallWedgeDiscoveryForEachQuadrant()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var session = new GameSession { Id = sessionId, Status = SessionStatus.SetupInProgress, ConnectionMode = "none" };

        _sessionRepositoryMock.Setup(r => r.GetByIdAsync(sessionId)).ReturnsAsync(session);
        _nodeRepositoryMock.Setup(r => r.GetBySessionIdAsync(sessionId)).ReturnsAsync(new List<MapNode>());

        // Mock returns
        _osmDiscoveryServiceMock.Setup(r => r.GetRandomNodesAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<double>()))
            .ReturnsAsync(new List<DiscoveryPoint> { new DiscoveryPoint(0, 0) });
        _osmDiscoveryServiceMock.Setup(r => r.GetRandomNodesInWedgeAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double>()))
            .ReturnsAsync(new List<DiscoveryPoint> { new DiscoveryPoint(0, 0) });

        var request = new NodeGenerationRequest
        {
            SessionId = sessionId,
            GameMode = "quadrant",
            NodeCount = 20,
            Radius = 5000
        };

        // Act
        await _service.GenerateNodesAsync(request);

        // Assert
        // 1 call for Hub (GetRandomNodesAsync)
        _osmDiscoveryServiceMock.Verify(r => r.GetRandomNodesAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<double>()), Times.Once);

        // 4 calls for quadrants (North, East, South, West), each with the correct angle bounds
        _osmDiscoveryServiceMock.Verify(r => r.GetRandomNodesInWedgeAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), 315, 45,  It.IsAny<int>(), It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double>()), Times.Once);
        _osmDiscoveryServiceMock.Verify(r => r.GetRandomNodesInWedgeAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), 45,  135, It.IsAny<int>(), It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double>()), Times.Once);
        _osmDiscoveryServiceMock.Verify(r => r.GetRandomNodesInWedgeAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), 135, 225, It.IsAny<int>(), It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double>()), Times.Once);
        _osmDiscoveryServiceMock.Verify(r => r.GetRandomNodesInWedgeAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), 225, 315, It.IsAny<int>(), It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double>()), Times.Once);
    }

    [Fact]
    public async Task GenerateNodesAsync_QuadrantMode_ShouldPassHubRadiusAsMinRadiusForEachQuadrant()
    {
        // The hub queries the full circle within hubRadius and adds those nodes to seenPoints.
        // Quadrant queries must therefore start BEYOND hubRadius to avoid finding only nodes
        // that the hub already claimed — which would leave quadrants empty in sparse areas.
        var sessionId = Guid.NewGuid();
        const double gameRadius = 10_000;
        const double expectedHubRadius = gameRadius * 0.25; // 2500m

        var session = new GameSession { Id = sessionId, Status = SessionStatus.SetupInProgress, ConnectionMode = "none" };
        _sessionRepositoryMock.Setup(r => r.GetByIdAsync(sessionId)).ReturnsAsync(session);
        _nodeRepositoryMock.Setup(r => r.GetBySessionIdAsync(sessionId)).ReturnsAsync(new List<MapNode>());

        _osmDiscoveryServiceMock
            .Setup(r => r.GetRandomNodesAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<double>()))
            .ReturnsAsync(new List<DiscoveryPoint>());
        _osmDiscoveryServiceMock
            .Setup(r => r.GetRandomNodesInWedgeAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double>()))
            .ReturnsAsync(new List<DiscoveryPoint>());

        var request = new NodeGenerationRequest
        {
            SessionId = sessionId,
            GameMode = "quadrant",
            NodeCount = 20,
            Radius = gameRadius
        };

        await _service.GenerateNodesAsync(request);

        // Each quadrant call must receive the hub radius as minRadiusMeters so probes
        // target the outer ring and don't compete with hub node discovery.
        _osmDiscoveryServiceMock.Verify(
            r => r.GetRandomNodesInWedgeAsync(
                It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(),
                It.IsAny<double>(), It.IsAny<double>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<double>(),
                expectedHubRadius),
            Times.Exactly(4));
    }

    [Fact]
    public async Task GenerateNodesAsync_QuadrantMode_EastNodesBeyondHubRadius_GetEastTag()
    {
        // Nodes found by the east quadrant query that are beyond the hub radius must be
        // tagged "East", not "Hub". This verifies the region-tag logic is correct.
        var sessionId = Guid.NewGuid();
        const double gameRadius = 10_000;
        const double hubRadius = gameRadius * 0.25; // 2500m

        // A point that is clearly east and clearly beyond hub radius.
        // Pittsburgh area: center at (40.44, -79.99), east node ~5 km east.
        const double centerLat = 40.44;
        const double centerLon = -79.99;
        // ~5 km east: lon += 5000 / (111132 * cos(40.44°)) ≈ +0.0589°
        var eastNode = new DiscoveryPoint(centerLon + 0.06, centerLat); // ~5.5 km east

        var session = new GameSession
        {
            Id = sessionId,
            Status = SessionStatus.SetupInProgress,
            ConnectionMode = "none",
        };
        _sessionRepositoryMock.Setup(r => r.GetByIdAsync(sessionId)).ReturnsAsync(session);
        _nodeRepositoryMock.Setup(r => r.GetBySessionIdAsync(sessionId)).ReturnsAsync(new List<MapNode>());

        // Hub returns nothing; only the east quadrant returns a node.
        _osmDiscoveryServiceMock
            .Setup(r => r.GetRandomNodesAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<double>()))
            .ReturnsAsync(new List<DiscoveryPoint>());
        // East quadrant (startDeg=45) returns the east node; all other wedge calls return empty.
        _osmDiscoveryServiceMock
            .Setup(r => r.GetRandomNodesInWedgeAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double>()))
            .Returns((double lat2, double lon2, double rad, double startDeg, double endDeg, int cnt, string md, double bias, double minRad) =>
                Task.FromResult(startDeg == 45
                    ? new List<DiscoveryPoint> { eastNode }
                    : new List<DiscoveryPoint>()));

        List<MapNode>? capturedNodes = null;
        _nodeRepositoryMock
            .Setup(r => r.CreateRangeAsync(It.IsAny<IEnumerable<MapNode>>()))
            .Callback<IEnumerable<MapNode>>(nodes => capturedNodes = nodes.ToList())
            .Returns(Task.CompletedTask);

        var request = new NodeGenerationRequest
        {
            SessionId = sessionId,
            CenterLat = centerLat,
            CenterLon = centerLon,
            GameMode = "quadrant",
            NodeCount = 20,
            Radius = gameRadius
        };

        await _service.GenerateNodesAsync(request);

        Assert.NotNull(capturedNodes);
        var east = capturedNodes!.Single(n => n.RegionTag == "East");
        Assert.True(
            Math.Abs(east.Location!.X - eastNode.Lon) < 1e-9 &&
            Math.Abs(east.Location!.Y - eastNode.Lat) < 1e-9,
            "The east-quadrant node should have been stored with the correct coordinates.");
    }

    [Fact]
    public async Task GenerateNodesAsync_RadiusMode_ShouldCallDiscoveryTwice()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var session = new GameSession { Id = sessionId, Status = SessionStatus.SetupInProgress, ConnectionMode = "none" };

        _sessionRepositoryMock.Setup(r => r.GetByIdAsync(sessionId)).ReturnsAsync(session);
        _nodeRepositoryMock.Setup(r => r.GetBySessionIdAsync(sessionId)).ReturnsAsync(new List<MapNode>());

        _osmDiscoveryServiceMock.Setup(r => r.GetRandomNodesAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<double>()))
            .ReturnsAsync(new List<DiscoveryPoint> { new DiscoveryPoint(0, 0) });

        var request = new NodeGenerationRequest
        {
            SessionId = sessionId,
            GameMode = "radius",
            NodeCount = 10
        };

        // Act
        await _service.GenerateNodesAsync(request);

        // Assert
        // 1 call for Hub, 1 call for Outer
        _osmDiscoveryServiceMock.Verify(r => r.GetRandomNodesAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<double>()), Times.Exactly(2));
    }

    [Fact]
    public async Task GenerateNodesAsync_ArchipelagoMode_ShouldUseUniformDistribution()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var session = new GameSession { Id = sessionId, Status = SessionStatus.SetupInProgress, ConnectionMode = "none" };

        _sessionRepositoryMock.Setup(r => r.GetByIdAsync(sessionId)).ReturnsAsync(session);
        _nodeRepositoryMock.Setup(r => r.GetBySessionIdAsync(sessionId)).ReturnsAsync(new List<MapNode>());

        _osmDiscoveryServiceMock.Setup(r => r.GetRandomNodesAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<double>()))
            .ReturnsAsync(new List<DiscoveryPoint> { new DiscoveryPoint(0, 0) });

        var request = new NodeGenerationRequest
        {
            SessionId = sessionId,
            GameMode = "archipelago",
            NodeCount = 10
        };

        // Act
        await _service.GenerateNodesAsync(request);

        // Assert
        // In "archipelago" mode, it initially doesn't know the progression mode, so it does Hub + Outer uniform
        _osmDiscoveryServiceMock.Verify(r => r.GetRandomNodesAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<double>()), Times.Exactly(2));
        _osmDiscoveryServiceMock.Verify(r => r.GetRandomNodesInWedgeAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double>()), Times.Never);
    }
}
