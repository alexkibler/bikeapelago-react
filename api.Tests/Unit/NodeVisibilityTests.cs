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

namespace Bikeapelago.Api.Tests.Unit;

public class NodeVisibilityTests
{
    private readonly Mock<IOsmDiscoveryService> _osmDiscoveryServiceMock;
    private readonly Mock<IMapNodeRepository> _nodeRepositoryMock;
    private readonly Mock<IGameSessionRepository> _sessionRepositoryMock;
    private readonly NodeGenerationService _generationService;

    public NodeVisibilityTests()
    {
        _osmDiscoveryServiceMock = new Mock<IOsmDiscoveryService>();
        _nodeRepositoryMock = new Mock<IMapNodeRepository>();
        _sessionRepositoryMock = new Mock<IGameSessionRepository>();

        _generationService = new NodeGenerationService(
            _osmDiscoveryServiceMock.Object,
            _nodeRepositoryMock.Object,
            _sessionRepositoryMock.Object,
            Mock.Of<ILogger<NodeGenerationService>>());
    }

    [Fact]
    public async Task GenerateNodesAsync_StrictHubBoundary_ShouldTagCorrectly()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var centerLat = 40.4406;
        var centerLon = -79.9959;
        var radius = 1000.0; // 1km radius
        
        var session = new GameSession { Id = sessionId, Status = SessionStatus.SetupInProgress };
        _sessionRepositoryMock.Setup(r => r.GetByIdAsync(sessionId)).ReturnsAsync(session);
        _nodeRepositoryMock.Setup(r => r.GetBySessionIdAsync(sessionId)).ReturnsAsync(new List<MapNode>());

        // 1 degree lat is ~111,132m. 
        // 0.0022 degrees lat is ~244m (Inside Hub - 25% of 1000m is 250m)
        // 0.0023 degrees lat is ~255m (Outside Hub)
        var pInside = new DiscoveryPoint(centerLon, centerLat + 0.0022);
        var pOutside = new DiscoveryPoint(centerLon, centerLat + 0.0023);

        // For this test, we skip the Hub distribution and just mock the "Outer" distribution
        // so we can see how the tags are assigned to random points.
        _osmDiscoveryServiceMock.Setup(r => r.GetRandomNodesAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<double>()))
            .ReturnsAsync(new List<DiscoveryPoint> { pInside, pOutside });

        var request = new NodeGenerationRequest
        {
            SessionId = sessionId,
            CenterLat = centerLat,
            CenterLon = centerLon,
            Radius = radius,
            NodeCount = 2,
            GameMode = "radius"
        };

        List<MapNode> capturedNodes = null!;
        _nodeRepositoryMock.Setup(r => r.CreateRangeAsync(It.IsAny<IEnumerable<MapNode>>()))
            .Callback<IEnumerable<MapNode>>(nodes => capturedNodes = nodes.ToList())
            .Returns(Task.CompletedTask);

        // Act
        await _generationService.GenerateNodesAsync(request);

        // Assert
        Assert.NotNull(capturedNodes);
        
        var nodeInside = capturedNodes.OrderBy(n => CalculateHaversine(centerLat, centerLon, n.Lat!.Value, n.Lon!.Value)).First();
        var nodeOutside = capturedNodes.OrderByDescending(n => CalculateHaversine(centerLat, centerLon, n.Lat!.Value, n.Lon!.Value)).First();

        Assert.Equal("Hub", nodeInside.RegionTag);
        Assert.Equal("Available", nodeInside.State);

        Assert.NotEqual("Hub", nodeOutside.RegionTag);
        Assert.Equal("Hidden", nodeOutside.State);
    }

    private double CalculateHaversine(double lat1, double lon1, double lat2, double lon2)
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
